using System.Text.Json;
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
/// One-time passwords: POST tokens/verify-otp is anonymous and answers with a reset token (set-password),
/// so codes must be unpredictable, stored only as a keyed hash and protected by an attempt limit. The reset token is
/// generated only when the code is verified, so only the one who knows the code gets it.
/// </summary>
public class OneTimePasswordServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	private const string Username = "john.doe";
	private const string Email = "john.doe@example.com";
	private const string Host = "https://app.example.com";
	private const int MaxAttempts = 3;
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly IPasswordResetService _passwordResetService = Substitute.For<IPasswordResetService>();
	
	private readonly IOneTimePasswordRepository _repository = Substitute.For<IOneTimePasswordRepository>();
	
	private readonly List<OneTimePassword> _otps;
	
	private readonly Membership _membership;
	
	private int _resetTokenCounter;
	
	#endregion
	
	#region Constructors
	
	public OneTimePasswordServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._otps = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		// Like the repository's atomic FindOneAndUpdate: increments only below the limit, returns the updated document
		this._repository
			.TryReserveAttemptAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var otp = this._otps.FirstOrDefault(x => x.Id == callInfo.ArgAt<string>(0) && x.FailedAttempts < callInfo.ArgAt<int>(1));
				if (otp != null)
				{
					otp.FailedAttempts++;
				}
				
				return otp;
			});
		
		this._repository
			.ReleaseAttemptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var otp = this._otps.FirstOrDefault(x => x.Id == callInfo.ArgAt<string>(0) && x.FailedAttempts > 0);
				if (otp != null)
				{
					otp.FailedAttempts--;
				}
				
				return Task.CompletedTask;
			});
		
		this._membership = TestServiceFactory.CreateMembership();
		this._membership.Id = MembershipId;
		this._membership.OtpSettings = new OtpSettings
		{
			Host = Host,
			Policy = new OtpPasswordPolicy { Length = 6, ContainsDigits = true, ContainsLetters = false, ExpiresIn = 300, MaxAttempts = MaxAttempts }
		};
		
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(this._membership);
		this._userService.GetUserAsync(UserId, MembershipId, Arg.Any<CancellationToken>()).Returns(new User
		{
			Id = UserId,
			Username = Username,
			EmailAddress = Email,
			Role = "user",
			IsActive = true,
			MembershipId = MembershipId
		});
		
		this._passwordResetService
			.GenerateResetPasswordTokenAsync(Arg.Any<User>(), Arg.Any<Membership>(), Arg.Any<bool>(), Arg.Any<ResetPasswordToken.ResetPasswordTokenPurpose>(), Arg.Any<CancellationToken>())
			.Returns(_ => new ResetPasswordToken($"reset-token-{++this._resetTokenCounter}", TimeSpan.FromMinutes(5)));
	}
	
	#endregion
	
	#region Helpers
	
	private OneTimePasswordService CreateService()
	{
		return new OneTimePasswordService(
			this._membershipService,
			this._userService,
			this._passwordResetService,
			this._repository);
	}
	
	private Task<OneTimePassword> GenerateAsync(OneTimePasswordService service)
	{
		return service.GenerateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, UserId, TestContext.Current.CancellationToken);
	}
	
	private Task<ResetPasswordToken?> VerifyAsync(OneTimePasswordService service, string code, string username = Username, string? host = Host)
	{
		return service.VerifyOtpAsync(username, code, MembershipId, host, TestContext.Current.CancellationToken);
	}
	
	private static string WrongCode(string code)
	{
		return code == "999999" ? "888888" : "999999";
	}
	
	#endregion
	
	#region Generate
	
	[Fact]
	public async Task GenerateAsync_ReturnsACodeMatchingThePolicy()
	{
		var otp = await this.GenerateAsync(this.CreateService());
		
		Assert.NotNull(otp.Password);
		Assert.Equal(6, otp.Password.Length);
		Assert.All(otp.Password, x => Assert.True(char.IsAsciiDigit(x)));
		Assert.NotEqual('0', otp.Password[0]);
		Assert.Equal(300, otp.ExpiresIn);
		Assert.Equal(otp.CreatedAt.AddSeconds(300), otp.ExpireTime);
	}
	
	[Fact]
	public async Task GenerateAsync_GeneratesNoResetToken()
	{
		// Regression: the reset token was generated with the code, returned to the caller and stored in plain text,
		// so the one who generated the code (or a reader of the database) could set the password without the code
		var otp = await this.GenerateAsync(this.CreateService());
		
		await this._passwordResetService.DidNotReceiveWithAnyArgs().GenerateResetPasswordTokenAsync(default!, default!);
		Assert.DoesNotContain("token", JsonSerializer.Serialize(otp));
		Assert.False(Assert.Single(this._otps).ToBsonDocument().Contains("token"));
	}
	
	[Fact]
	public async Task GenerateAsync_ForInactiveUser_ThrowsUserInactive()
	{
		(await this._userService.GetUserAsync(UserId, MembershipId, TestContext.Current.CancellationToken))!.IsActive = false;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateAsync(this.CreateService()));
		
		Assert.Equal("UserInactive", exception.ErrorCode);
		Assert.Empty(this._otps);
	}
	
	[Fact]
	public async Task GenerateAsync_WithLettersPolicy_ReturnsUppercaseLetters()
	{
		this._membership.OtpSettings!.Policy = new OtpPasswordPolicy { Length = 8, ContainsLetters = true, ContainsDigits = false, ExpiresIn = 300 };
		
		var otp = await this.GenerateAsync(this.CreateService());
		
		Assert.Equal(8, otp.Password!.Length);
		Assert.All(otp.Password, x => Assert.True(char.IsAsciiLetterUpper(x)));
	}
	
	[Fact]
	public async Task GenerateAsync_ProducesUnpredictableCodes()
	{
		// Regression: codes came from new Random(DateTime.Now.Microsecond), i.e. at most 1000 codes per policy
		var service = this.CreateService();
		var codes = new HashSet<string>();
		for (var i = 0; i < 300; i++)
		{
			codes.Add((await this.GenerateAsync(service)).Password!);
		}
		
		// 300 random 6-digit codes out of 900,000 collide with a probability of about 5%
		Assert.True(codes.Count >= 298, $"{300 - codes.Count} duplicate codes");
	}
	
	[Fact]
	public async Task GenerateAsync_StoresOnlyAKeyedHashOfTheCode()
	{
		var otp = await this.GenerateAsync(this.CreateService());
		
		var stored = Assert.Single(this._otps);
		var document = stored.ToBsonDocument();
		Assert.False(document.Contains("password"));
		Assert.NotEqual(otp.Password, document["password_hash"].AsString);
		Assert.DoesNotContain(otp.Password!, document.ToJson());
	}
	
	[Fact]
	public async Task GenerateAsync_ReplacesThePreviousOneTimePassword()
	{
		var service = this.CreateService();
		var first = await this.GenerateAsync(service);
		var second = await this.GenerateAsync(service);
		
		Assert.Equal(second.Id, Assert.Single(this._otps).Id);
		if (first.Password != second.Password)
		{
			Assert.Null(await this.VerifyAsync(service, first.Password!));
		}
		
		Assert.NotNull(await this.VerifyAsync(service, second.Password!));
	}
	
	[Fact]
	public async Task GenerateAsync_WithoutOtpPolicy_ThrowsOtpNotConfiguredYet()
	{
		this._membership.OtpSettings = null;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.GenerateAsync(this.CreateService()));
		
		Assert.Equal("OtpNotConfiguredYet", exception.ErrorCode);
	}
	
	#endregion
	
	#region Verify
	
	[Theory]
	[InlineData(Username)]
	[InlineData(Email)]
	public async Task VerifyOtpAsync_WithCorrectCode_ReturnsTheResetToken(string username)
	{
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		var resetToken = await this.VerifyAsync(service, otp.Password!, username);
		
		Assert.Equal("reset-token-1", resetToken?.Token);
		await this._passwordResetService.Received(1).GenerateResetPasswordTokenAsync(
			Arg.Is<User>(x => x.Id == UserId),
			this._membership,
			true,
			ResetPasswordToken.ResetPasswordTokenPurpose.OneTimePassword,
			Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task VerifyOtpAsync_ComparesTheCodeCaseInsensitively()
	{
		this._membership.OtpSettings!.Policy = new OtpPasswordPolicy { Length = 6, ContainsLetters = true, ContainsDigits = false, ExpiresIn = 300 };
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		Assert.NotNull(await this.VerifyAsync(service, otp.Password!.ToLowerInvariant()));
	}
	
	[Fact]
	public async Task VerifyOtpAsync_ConsumesTheOneTimePassword()
	{
		// Single use: the reset token is handed out once
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		Assert.NotNull(await this.VerifyAsync(service, otp.Password!));
		Assert.Empty(this._otps);
		Assert.Null(await this.VerifyAsync(service, otp.Password!));
	}
	
	[Fact]
	public async Task VerifyOtpAsync_WithWrongCode_ReturnsNullAndCountsTheAttempt()
	{
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		Assert.Null(await this.VerifyAsync(service, WrongCode(otp.Password!)));
		
		Assert.Equal(1, Assert.Single(this._otps).FailedAttempts);
	}
	
	[Fact]
	public async Task VerifyOtpAsync_WhenTheAttemptsAreUsedUp_RejectsEvenTheCorrectCode()
	{
		// Attempts are reserved before the code is compared: parallel requests that already reserved all attempts
		// leave none for further guesses, so no more than max_attempts guesses are ever evaluated
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		Assert.Single(this._otps).FailedAttempts = MaxAttempts;
		
		Assert.Null(await this.VerifyAsync(service, otp.Password!));
		Assert.Empty(this._otps);
	}
	
	[Fact]
	public async Task VerifyOtpAsync_AfterMaxFailedAttempts_DeletesTheOneTimePassword()
	{
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		for (var i = 0; i < MaxAttempts; i++)
		{
			Assert.Null(await this.VerifyAsync(service, WrongCode(otp.Password!)));
		}
		
		Assert.Empty(this._otps);
		Assert.Null(await this.VerifyAsync(service, otp.Password!));
	}
	
	[Fact]
	public async Task VerifyOtpAsync_BelowMaxFailedAttempts_StillAcceptsTheCorrectCode()
	{
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		for (var i = 0; i < MaxAttempts - 1; i++)
		{
			await this.VerifyAsync(service, WrongCode(otp.Password!));
		}
		
		Assert.NotNull(await this.VerifyAsync(service, otp.Password!));
	}
	
	[Fact]
	public async Task VerifyOtpAsync_WithExpiredOneTimePassword_ThrowsOtpExpiredOnlyForTheCorrectCode()
	{
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		Assert.Single(this._otps).ExpireTime = DateTime.UtcNow.AddSeconds(-1);
		
		Assert.Null(await this.VerifyAsync(service, WrongCode(otp.Password!)));
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.VerifyAsync(service, otp.Password!));
		
		Assert.Equal("OtpExpired", exception.ErrorCode);
		Assert.Empty(this._otps);
		await this._passwordResetService.DidNotReceiveWithAnyArgs().GenerateResetPasswordTokenAsync(default!, default!);
	}
	
	[Fact]
	public async Task VerifyOtpAsync_ForUserWithoutOneTimePassword_ReturnsNull()
	{
		Assert.Null(await this.VerifyAsync(this.CreateService(), "123456", "unknown.user"));
	}
	
	[Theory]
	[InlineData(null, "OtpHostRequired")]
	[InlineData("https://attacker.example.com", "OtpHostMismatch")]
	public async Task VerifyOtpAsync_WithWrongHost_Throws(string? host, string errorCode)
	{
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.VerifyAsync(service, otp.Password!, host: host));
		
		Assert.Equal(errorCode, exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyOtpAsync_WithCodeOfAnotherUsersOneTimePassword_ReturnsNull()
	{
		// The hash binds the code to its user
		var service = this.CreateService();
		var otp = await this.GenerateAsync(service);
		var stored = Assert.Single(this._otps);
		this._otps.Add(new OneTimePassword
		{
			Id = ObjectId.GenerateNewId().ToString(),
			MembershipId = MembershipId,
			UserId = "5f8a1b2c3d4e5f6a7b8c9d02",
			Username = "jane.doe",
			EmailAddress = "jane.doe@example.com",
			PasswordHash = stored.PasswordHash,
			ExpiresIn = stored.ExpiresIn,
			CreatedAt = stored.CreatedAt,
			ExpireTime = stored.ExpireTime
		});
		
		Assert.Null(await this.VerifyAsync(service, otp.Password!, "jane.doe"));
	}
	
	#endregion
}