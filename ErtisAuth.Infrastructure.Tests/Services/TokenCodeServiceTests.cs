using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using MongoDB.Bson;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Token codes (device login, e.g. a smart TV): the device shows the user code and polls with the device code, which
/// only it knows; a signed-in user approves or denies the user code. Codes must be unpredictable, the user code alone
/// must never give a token, and a token is handed out only once.
/// </summary>
public class TokenCodeServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	private const string OtherUserId = "5f8a1b2c3d4e5f6a7b8c9d02";
	private const string PolicySlug = "tv-code";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly ITokenCodePolicyService _tokenCodePolicyService = Substitute.For<ITokenCodePolicyService>();
	
	private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly ITokenCodeRepository _repository = Substitute.For<ITokenCodeRepository>();
	
	private readonly List<TokenCode> _tokenCodes;
	
	private readonly Dictionary<string, User> _users = new();
	
	private readonly Membership _membership;
	
	private TokenCodePolicy _policy = new() { Name = "TV Code", Length = 6, ContainsDigits = true, ExpiresIn = 300, MembershipId = MembershipId };
	
	#endregion
	
	#region Constructors
	
	public TokenCodeServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._tokenCodes = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		this.SetupAtomicMethods();
		
		this._membership = TestServiceFactory.CreateMembership();
		this._membership.Id = MembershipId;
		this._membership.CodePolicy = PolicySlug;
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(this._membership);
		this._tokenCodePolicyService.GetBySlugAsync(PolicySlug, MembershipId, Arg.Any<CancellationToken>()).Returns(_ => this._policy);
		
		foreach (var userId in new[] { UserId, OtherUserId })
		{
			this._users[userId] = new User
			{
				Id = userId,
				Username = userId,
				EmailAddress = $"{userId}@example.com",
				Role = "user",
				IsActive = true,
				MembershipId = MembershipId
			};
			
			this._userService.GetUserAsync(userId, MembershipId, Arg.Any<CancellationToken>()).Returns(callInfo => this._users[callInfo.ArgAt<string>(0)]);
		}
		
		this._tokenService
			.GenerateTokenAsync(Arg.Any<User>(), MembershipId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new BearerToken($"access-token-of-{callInfo.ArgAt<User>(0).Id}", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2)));
		
		this._tokenService
			.GenerateScopedTokenAsync(Arg.Any<User>(), Arg.Any<string[]>(), MembershipId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new BearerToken($"scoped-token-of-{callInfo.ArgAt<User>(0).Id}", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2)));
	}
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// The atomic repository methods on the in-memory store, with the same filters as the MongoDB implementation.
	/// </summary>
	private void SetupAtomicMethods()
	{
		this._repository
			.TryDecideAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string[]?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var now = callInfo.ArgAt<DateTime>(5);
				var tokenCode = this._tokenCodes.FirstOrDefault(x =>
					x.UserCode == callInfo.ArgAt<string>(0) &&
					x.MembershipId == callInfo.ArgAt<string>(1) &&
					x.Status == TokenCodeStatus.Pending &&
					x.ExpireTime > now);
				
				if (tokenCode != null)
				{
					tokenCode.Status = callInfo.ArgAt<string>(2);
					tokenCode.UserId = callInfo.ArgAt<string>(3);
					tokenCode.Scopes = callInfo.ArgAt<string[]?>(4);
					tokenCode.DecidedAt = now;
				}
				
				return tokenCode;
			});
		
		this._repository
			.TryRegisterPollAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var now = callInfo.ArgAt<DateTime>(2);
				var tokenCode = this._tokenCodes.FirstOrDefault(x => x.Id == callInfo.ArgAt<string>(0));
				if (tokenCode == null || (tokenCode.LastPolledAt != null && tokenCode.LastPolledAt > now.AddSeconds(-callInfo.ArgAt<int>(1))))
				{
					return false;
				}
				
				tokenCode.LastPolledAt = now;
				return true;
			});
		
		this._repository
			.TryConsumeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var tokenCode = this._tokenCodes.FirstOrDefault(x => x.Id == callInfo.ArgAt<string>(0) && x.Status == TokenCodeStatus.Approved);
				if (tokenCode != null)
				{
					this._tokenCodes.Remove(tokenCode);
				}
				
				return tokenCode;
			});
	}
	
	private TokenCodeService CreateService()
	{
		return new TokenCodeService(this._membershipService, this._tokenCodePolicyService, this._tokenService, this._userService, this._eventService, this._repository);
	}
	
	private Task<TokenCodeWithDeviceCode> CreateCodeAsync(TokenCodeService service, ClientInfo? clientInfo = null)
	{
		return service.CreateAsync(MembershipId, clientInfo, TestContext.Current.CancellationToken);
	}
	
	private static Utilizer UserUtilizer(string userId)
	{
		return new Utilizer { Id = userId, Username = userId, Type = Utilizer.UtilizerType.User, MembershipId = MembershipId };
	}
	
	private Task<TokenCode> ApproveAsync(TokenCodeService service, string userCode, string userId = UserId, string[]? scopes = null)
	{
		var utilizer = UserUtilizer(userId);
		utilizer.Scopes = scopes;
		return service.ApproveAsync(userCode, MembershipId, utilizer, TestContext.Current.CancellationToken);
	}
	
	private Task<TokenCode> DenyAsync(TokenCodeService service, string userCode, string userId = UserId)
	{
		return service.DenyAsync(userCode, MembershipId, UserUtilizer(userId), TestContext.Current.CancellationToken);
	}
	
	private Task<BearerToken> GenerateTokenAsync(TokenCodeService service, string deviceCode)
	{
		return service.GenerateTokenAsync(deviceCode, MembershipId, TestContext.Current.CancellationToken);
	}
	
	/// <summary>
	/// Lets the next poll of the device through, as if the interval had passed.
	/// </summary>
	private void WaitForTheInterval()
	{
		foreach (var tokenCode in this._tokenCodes)
		{
			tokenCode.LastPolledAt = null;
		}
	}
	
	#endregion
	
	#region Create
	
	[Fact]
	public async Task CreateAsync_ReturnsACodeMatchingThePolicy()
	{
		var tokenCode = await this.CreateCodeAsync(this.CreateService());
		
		Assert.Equal(6, tokenCode.UserCode.Length);
		Assert.All(tokenCode.UserCode, x => Assert.True(char.IsAsciiDigit(x)));
		Assert.Equal(300, tokenCode.ExpiresIn);
		Assert.Equal(TokenCodeService.PollInterval, tokenCode.Interval);
		Assert.Equal(TokenCodeStatus.Pending, tokenCode.Status);
		Assert.Equal(tokenCode.CreatedAt.AddSeconds(300), tokenCode.ExpireTime);
		Assert.Equal(tokenCode.Id, Assert.Single(this._tokenCodes).Id);
	}
	
	[Fact]
	public async Task CreateAsync_StoresOnlyTheHashOfTheDeviceCode()
	{
		var tokenCode = await this.CreateCodeAsync(this.CreateService());
		
		// 32 random bytes, base64url
		Assert.Equal(43, tokenCode.DeviceCode.Length);
		var stored = Assert.Single(this._tokenCodes);
		Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tokenCode.DeviceCode))), stored.DeviceCodeHash);
		Assert.IsNotType<TokenCodeWithDeviceCode>(stored);
		Assert.DoesNotContain(tokenCode.DeviceCode, stored.ToBsonDocument().ToJson());
	}
	
	[Fact]
	public async Task CreateAsync_StoresTheClientInfoOfTheDevice()
	{
		await this.CreateCodeAsync(this.CreateService(), new ClientInfo { IPAddress = "203.0.113.42", UserAgent = "SmartTV/1.0" });
		
		var stored = Assert.Single(this._tokenCodes);
		Assert.Equal("203.0.113.42", stored.ClientInfo?.IPAddress);
		Assert.Equal("SmartTV/1.0", stored.ClientInfo?.UserAgent);
	}
	
	[Fact]
	public async Task CreateAsync_WithLettersPolicy_ReturnsUppercaseLetters()
	{
		this._policy = new TokenCodePolicy { Name = "TV Code", Length = 8, ContainsLetters = true, ExpiresIn = 300, MembershipId = MembershipId };
		
		var tokenCode = await this.CreateCodeAsync(this.CreateService());
		
		Assert.All(tokenCode.UserCode, x => Assert.True(char.IsAsciiLetterUpper(x)));
	}
	
	[Fact]
	public async Task CreateAsync_WithMixedPolicy_LeavesOutTheAmbiguousCharacters()
	{
		this._policy = new TokenCodePolicy { Name = "TV Code", Length = 12, ContainsLetters = true, ContainsDigits = true, ExpiresIn = 300, MembershipId = MembershipId };
		var service = this.CreateService();
		
		for (var i = 0; i < 200; i++)
		{
			var tokenCode = await this.CreateCodeAsync(service);
			Assert.DoesNotContain(tokenCode.UserCode, x => x is '0' or 'O' or '1' or 'I');
		}
	}
	
	[Fact]
	public async Task CreateAsync_ProducesUnpredictableCodes()
	{
		// Regression: codes came from new Random(DateTime.Now.Microsecond), i.e. at most 1000 codes per policy
		this._policy = new TokenCodePolicy { Name = "TV Code", Length = 8, ContainsLetters = true, ContainsDigits = true, ExpiresIn = 300, MembershipId = MembershipId };
		var service = this.CreateService();
		var userCodes = new HashSet<string>();
		var deviceCodes = new HashSet<string>();
		for (var i = 0; i < 300; i++)
		{
			var tokenCode = await this.CreateCodeAsync(service);
			userCodes.Add(tokenCode.UserCode);
			deviceCodes.Add(tokenCode.DeviceCode);
		}
		
		Assert.Equal(300, userCodes.Count);
		Assert.Equal(300, deviceCodes.Count);
	}
	
	[Fact]
	public async Task CreateAsync_WithoutCodePolicy_ThrowsTokenCodePolicyNotFound()
	{
		this._membership.CodePolicy = null;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateCodeAsync(this.CreateService()));
		
		Assert.Equal("TokenCodePolicyNotFound", exception.ErrorCode);
	}
	
	#endregion
	
	#region Get
	
	[Fact]
	public async Task GetByUserCodeAsync_ReturnsTheDeviceForTheApprovalScreen()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service, new ClientInfo { IPAddress = "203.0.113.42", UserAgent = "SmartTV/1.0" });
		
		var found = await service.GetByUserCodeAsync(tokenCode.UserCode, MembershipId, TestContext.Current.CancellationToken);
		
		Assert.Equal("SmartTV/1.0", found.ClientInfo?.UserAgent);
		Assert.Equal(TokenCodeStatus.Pending, found.Status);
	}
	
	[Fact]
	public async Task GetByUserCodeAsync_WithExpiredCode_ThrowsTokenCodeNotFound()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		Assert.Single(this._tokenCodes).ExpireTime = DateTime.UtcNow.AddSeconds(-1);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => service.GetByUserCodeAsync(tokenCode.UserCode, MembershipId, TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenCodeNotFound", exception.ErrorCode);
	}
	
	#endregion
	
	#region Approve & Deny
	
	[Fact]
	public async Task ApproveAsync_RecordsTheApprovingUserWithoutGeneratingAToken()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		
		await this.ApproveAsync(service, tokenCode.UserCode);
		
		var stored = Assert.Single(this._tokenCodes);
		Assert.Equal(TokenCodeStatus.Approved, stored.Status);
		Assert.Equal(UserId, stored.UserId);
		Assert.NotNull(stored.DecidedAt);
		// The token is generated when the device gets it, so no token is stored with the code
		await this._tokenService.DidNotReceiveWithAnyArgs().GenerateTokenAsync(default(User)!, default!);
	}
	
	[Fact]
	public async Task ApproveAsync_IgnoresCaseAndSeparators()
	{
		this._policy = new TokenCodePolicy { Name = "TV Code", Length = 6, ContainsLetters = true, ExpiresIn = 300, MembershipId = MembershipId };
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		var typed = $"{tokenCode.UserCode[..3].ToLowerInvariant()}- {tokenCode.UserCode[3..].ToLowerInvariant()}";
		
		await this.ApproveAsync(service, typed);
		
		Assert.Equal(TokenCodeStatus.Approved, Assert.Single(this._tokenCodes).Status);
	}
	
	[Fact]
	public async Task ApproveAsync_WhenAlreadyApproved_IsRejectedAndKeepsTheFirstUser()
	{
		// Attack: approving the code shown on the victim's device with the attacker's account
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.ApproveAsync(service, tokenCode.UserCode);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.ApproveAsync(service, tokenCode.UserCode, OtherUserId));
		
		Assert.Equal("TokenCodeAlreadyAuthorized", exception.ErrorCode);
		Assert.Equal(UserId, Assert.Single(this._tokenCodes).UserId);
	}
	
	[Fact]
	public async Task ApproveAsync_AfterDenial_IsRejected()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.DenyAsync(service, tokenCode.UserCode);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.ApproveAsync(service, tokenCode.UserCode));
		
		Assert.Equal("TokenCodeAlreadyAuthorized", exception.ErrorCode);
		Assert.Equal(TokenCodeStatus.Denied, Assert.Single(this._tokenCodes).Status);
	}
	
	[Fact]
	public async Task ApproveAsync_WithUnknownCode_ThrowsTokenCodeNotFound()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.ApproveAsync(this.CreateService(), "999999"));
		
		Assert.Equal("TokenCodeNotFound", exception.ErrorCode);
	}
	
	[Fact]
	public async Task ApproveAsync_WithExpiredCode_ThrowsTokenCodeExpired()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		Assert.Single(this._tokenCodes).ExpireTime = DateTime.UtcNow.AddSeconds(-1);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.ApproveAsync(service, tokenCode.UserCode));
		
		Assert.Equal("TokenCodeExpired", exception.ErrorCode);
	}
	
	[Fact]
	public async Task ApproveAsync_WithCodeOfAnotherMembership_ThrowsTokenCodeNotFound()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		Assert.Single(this._tokenCodes).MembershipId = "5f8a1b2c3d4e5f6a7b8c9dff";
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.ApproveAsync(service, tokenCode.UserCode));
		
		Assert.Equal("TokenCodeNotFound", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData(true, ErtisAuthEventType.TokenCodeApproved)]
	[InlineData(false, ErtisAuthEventType.TokenCodeDenied)]
	public async Task Decision_FiresAnEventWithoutTheDeviceCode(bool approve, ErtisAuthEventType expectedEventType)
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service, new ClientInfo { UserAgent = "SmartTV/1.0" });
		object? document = null;
		this._eventService
			.When(x => x.FireEventAsync(expectedEventType, Arg.Any<Utilizer>(), MembershipId, Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>()))
			.Do(x => document = x.ArgAt<object?>(3));
		
		await (approve ? this.ApproveAsync(service, tokenCode.UserCode) : this.DenyAsync(service, tokenCode.UserCode));
		
		var json = JsonSerializer.Serialize(document);
		Assert.Contains(tokenCode.UserCode, json);
		Assert.Contains("SmartTV/1.0", json);
		Assert.DoesNotContain(tokenCode.DeviceCode, json);
		Assert.DoesNotContain(Assert.Single(this._tokenCodes).DeviceCodeHash!, json);
	}
	
	#endregion
	
	#region Generate Token
	
	[Fact]
	public async Task GenerateTokenAsync_WithTheUserCode_IsRejected()
	{
		// Attack: someone who sees the code on the screen polls with it, to get the token before the device
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.ApproveAsync(service, tokenCode.UserCode);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.UserCode));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
		Assert.Single(this._tokenCodes);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_BeforeApproval_ThrowsUnauthorizedTokenCode()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		
		Assert.Equal("UnauthorizedTokenCode", exception.ErrorCode);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_AfterApproval_ReturnsATokenOfTheApprovingUserOnlyOnce()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service, new ClientInfo { IPAddress = "203.0.113.42", UserAgent = "SmartTV/1.0" });
		await this.ApproveAsync(service, tokenCode.UserCode);
		
		var token = await this.GenerateTokenAsync(service, tokenCode.DeviceCode);
		
		Assert.Equal($"access-token-of-{UserId}", token.AccessToken);
		// The session shows the device, not the browser of the approving user
		await this._tokenService.Received(1).GenerateTokenAsync(Arg.Is<User>(x => x.Id == UserId), MembershipId, "203.0.113.42", "SmartTV/1.0", Arg.Any<bool>(), Arg.Any<CancellationToken>());
		Assert.Empty(this._tokenCodes);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_ApprovedWithAScopedToken_ReturnsATokenWithTheSameScopes()
	{
		// Regression: a scoped token (with tokens.create) approved a device, which got an unscoped token of the user
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service, new ClientInfo { IPAddress = "203.0.113.42", UserAgent = "SmartTV/1.0" });
		await this.ApproveAsync(service, tokenCode.UserCode, scopes: ["tokens.create", "users.read"]);
		
		var token = await this.GenerateTokenAsync(service, tokenCode.DeviceCode);
		
		Assert.Equal($"scoped-token-of-{UserId}", token.AccessToken);
		await this._tokenService.Received(1).GenerateScopedTokenAsync(
			Arg.Is<User>(x => x.Id == UserId),
			Arg.Is<string[]>(x => x.SequenceEqual(new[] { "tokens.create", "users.read" })),
			MembershipId,
			"203.0.113.42",
			"SmartTV/1.0",
			Arg.Any<CancellationToken>());
		await this._tokenService.DidNotReceiveWithAnyArgs().GenerateTokenAsync(default(User)!, default!);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_ApprovedWithAnUnscopedToken_ReturnsAnUnscopedToken()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.ApproveAsync(service, tokenCode.UserCode);
		
		await this.GenerateTokenAsync(service, tokenCode.DeviceCode);
		
		await this._tokenService.DidNotReceiveWithAnyArgs().GenerateScopedTokenAsync(default!, default!, default!);
	}
	
	[Fact]
	public async Task DenyAsync_WithAScopedToken_StoresNoScopes()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		var utilizer = UserUtilizer(UserId);
		utilizer.Scopes = ["tokens.create"];
		
		await service.DenyAsync(tokenCode.UserCode, MembershipId, utilizer, TestContext.Current.CancellationToken);
		
		Assert.Null(Assert.Single(this._tokenCodes).Scopes);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_AfterDenial_ThrowsTokenCodeDenied()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.DenyAsync(service, tokenCode.UserCode);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		
		Assert.Equal("TokenCodeDenied", exception.ErrorCode);
		await this._tokenService.DidNotReceiveWithAnyArgs().GenerateTokenAsync(default(User)!, default!);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_PolledTooOften_ThrowsTokenCodeSlowDown()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		await this.ApproveAsync(service, tokenCode.UserCode);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		
		Assert.Equal("TokenCodeSlowDown", exception.ErrorCode);
		this.WaitForTheInterval();
		Assert.Equal($"access-token-of-{UserId}", (await this.GenerateTokenAsync(service, tokenCode.DeviceCode)).AccessToken);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithExpiredCode_ThrowsTokenCodeExpired()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.ApproveAsync(service, tokenCode.UserCode);
		Assert.Single(this._tokenCodes).ExpireTime = DateTime.UtcNow.AddSeconds(-1);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		
		Assert.Equal("TokenCodeExpired", exception.ErrorCode);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WhenTheApprovingUserWasDeactivated_ThrowsUserInactive()
	{
		var service = this.CreateService();
		var tokenCode = await this.CreateCodeAsync(service);
		await this.ApproveAsync(service, tokenCode.UserCode);
		this._users[UserId].IsActive = false;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.DeviceCode));
		
		Assert.Equal("UserInactive", exception.ErrorCode);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithUnknownDeviceCode_ThrowsInvalidToken()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(this.CreateService(), "unknown-device-code"));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	#endregion
}
