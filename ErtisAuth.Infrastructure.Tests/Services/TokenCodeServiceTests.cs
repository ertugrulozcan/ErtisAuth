using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
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
/// Token codes (device login, e.g. a smart TV): the device gets a code, a signed-in user approves it, the device polls
/// the anonymous generate-token endpoint. Codes must be unpredictable and a token is handed out only once.
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
	
	private readonly ITokenCodeRepository _repository = Substitute.For<ITokenCodeRepository>();
	
	private readonly List<TokenCode> _tokenCodes;
	
	private readonly Membership _membership;
	
	private TokenCodePolicy _policy = new() { Name = "TV Code", Length = 6, ContainsDigits = true, ExpiresIn = 300, MembershipId = MembershipId };
	
	#endregion
	
	#region Constructors
	
	public TokenCodeServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._tokenCodes = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		this._membership = TestServiceFactory.CreateMembership();
		this._membership.Id = MembershipId;
		this._membership.CodePolicy = PolicySlug;
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(this._membership);
		this._tokenCodePolicyService.GetBySlugAsync(PolicySlug, MembershipId, Arg.Any<CancellationToken>()).Returns(_ => this._policy);
		
		foreach (var userId in new[] { UserId, OtherUserId })
		{
			this._userService.GetUserAsync(userId, MembershipId, Arg.Any<CancellationToken>()).Returns(new User
			{
				Id = userId,
				Username = userId,
				EmailAddress = $"{userId}@example.com",
				Role = "user",
				MembershipId = MembershipId
			});
		}
		
		this._tokenService
			.GenerateTokenAsync(Arg.Any<User>(), MembershipId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => new BearerToken($"access-token-of-{callInfo.ArgAt<User>(0).Id}", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2)));
	}
	
	#endregion
	
	#region Helpers
	
	private TokenCodeService CreateService()
	{
		return new TokenCodeService(this._membershipService, this._tokenCodePolicyService, this._tokenService, this._userService, this._repository);
	}
	
	private static Utilizer UserUtilizer(string userId)
	{
		return new Utilizer { Id = userId, Username = userId, Type = Utilizer.UtilizerType.User, MembershipId = MembershipId };
	}
	
	private Task<TokenCode> AuthorizeAsync(TokenCodeService service, string code, string userId = UserId)
	{
		return service.AuthorizeCodeAsync(code, MembershipId, UserUtilizer(userId), TestContext.Current.CancellationToken);
	}
	
	private Task<BearerToken> GenerateTokenAsync(TokenCodeService service, string code)
	{
		return service.GenerateTokenAsync(code, MembershipId, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Create
	
	[Fact]
	public async Task CreateAsync_ReturnsACodeMatchingThePolicy()
	{
		var tokenCode = await this.CreateService().CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		
		Assert.NotNull(tokenCode.Code);
		Assert.Equal(6, tokenCode.Code.Length);
		Assert.All(tokenCode.Code, x => Assert.True(char.IsAsciiDigit(x)));
		Assert.Equal(300, tokenCode.ExpiresIn);
		Assert.Null(tokenCode.Token);
		Assert.Equal(tokenCode.Id, Assert.Single(this._tokenCodes).Id);
	}
	
	[Fact]
	public async Task CreateAsync_WithLettersPolicy_ReturnsUppercaseLetters()
	{
		this._policy = new TokenCodePolicy { Name = "TV Code", Length = 8, ContainsLetters = true, ExpiresIn = 300, MembershipId = MembershipId };
		
		var tokenCode = await this.CreateService().CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		
		Assert.All(tokenCode.Code!, x => Assert.True(char.IsAsciiLetterUpper(x)));
	}
	
	[Fact]
	public async Task CreateAsync_ProducesUnpredictableCodes()
	{
		// Regression: codes came from new Random(DateTime.Now.Microsecond), i.e. at most 1000 codes per policy, and
		// generate-token is anonymous: anyone could poll all of them and collect the tokens of approving users
		var service = this.CreateService();
		var codes = new HashSet<string>();
		for (var i = 0; i < 300; i++)
		{
			codes.Add((await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken)).Code!);
		}
		
		// The service regenerates a code already in use, so all 300 are distinct
		Assert.Equal(300, codes.Count);
	}
	
	[Fact]
	public async Task CreateAsync_WithoutCodePolicy_ThrowsTokenCodePolicyNotFound()
	{
		this._membership.CodePolicy = null;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateService().CreateAsync(MembershipId, TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenCodePolicyNotFound", exception.ErrorCode);
	}
	
	#endregion
	
	#region Authorize
	
	[Fact]
	public async Task AuthorizeCodeAsync_AssignsATokenOfTheApprovingUser()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		
		await this.AuthorizeAsync(service, tokenCode.Code!);
		
		var stored = Assert.Single(this._tokenCodes);
		Assert.Equal(UserId, stored.UserId);
		Assert.Equal($"access-token-of-{UserId}", stored.Token?.AccessToken);
	}
	
	[Fact]
	public async Task AuthorizeCodeAsync_WhenAlreadyAuthorized_IsRejectedAndKeepsTheFirstToken()
	{
		// Attack: approving the code shown on the victim's device with the attacker's account
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		await this.AuthorizeAsync(service, tokenCode.Code!);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.AuthorizeAsync(service, tokenCode.Code!, OtherUserId));
		
		Assert.Equal("TokenCodeAlreadyAuthorized", exception.ErrorCode);
		Assert.Equal(UserId, Assert.Single(this._tokenCodes).UserId);
	}
	
	[Fact]
	public async Task AuthorizeCodeAsync_WithUnknownCode_ThrowsInvalidTokenCode()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.AuthorizeAsync(this.CreateService(), "000000"));
		
		Assert.Equal("InvalidTokenCode", exception.ErrorCode);
	}
	
	[Fact]
	public async Task AuthorizeCodeAsync_WithExpiredCode_ThrowsTokenCodeExpired()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		Assert.Single(this._tokenCodes).CreatedAt = DateTime.UtcNow.AddHours(-1);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.AuthorizeAsync(service, tokenCode.Code!));
		
		Assert.Equal("TokenCodeExpired", exception.ErrorCode);
	}
	
	[Fact]
	public async Task AuthorizeCodeAsync_WithCodeOfAnotherMembership_ThrowsInvalidTokenCode()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		Assert.Single(this._tokenCodes).MembershipId = "5f8a1b2c3d4e5f6a7b8c9dff";
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.AuthorizeAsync(service, tokenCode.Code!));
		
		Assert.Equal("InvalidTokenCode", exception.ErrorCode);
	}
	
	#endregion
	
	#region Generate Token
	
	[Fact]
	public async Task GenerateTokenAsync_BeforeApproval_ThrowsUnauthorizedTokenCode()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.Code!));
		
		Assert.Equal("UnauthorizedTokenCode", exception.ErrorCode);
		Assert.Single(this._tokenCodes);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_AfterApproval_ReturnsTheTokenOnlyOnce()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		await this.AuthorizeAsync(service, tokenCode.Code!);
		
		var token = await this.GenerateTokenAsync(service, tokenCode.Code!);
		
		Assert.Equal($"access-token-of-{UserId}", token.AccessToken);
		Assert.Empty(this._tokenCodes);
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.Code!));
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithExpiredCode_ThrowsTokenCodeExpired()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		await this.AuthorizeAsync(service, tokenCode.Code!);
		Assert.Single(this._tokenCodes).CreatedAt = DateTime.UtcNow.AddHours(-1);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.Code!));
		
		Assert.Equal("TokenCodeExpired", exception.ErrorCode);
	}
	
	[Fact]
	public async Task GenerateTokenAsync_WithExpiredToken_ThrowsTokenWasExpired()
	{
		var service = this.CreateService();
		var tokenCode = await service.CreateAsync(MembershipId, TestContext.Current.CancellationToken);
		await this.AuthorizeAsync(service, tokenCode.Code!);
		Assert.Single(this._tokenCodes).Token = new BearerToken("old-token", TimeSpan.FromMinutes(1), createdAt: DateTime.UtcNow.AddHours(-1));
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateTokenAsync(service, tokenCode.Code!));
		
		Assert.Equal("TokenWasExpired", exception.ErrorCode);
	}
	
	#endregion
}