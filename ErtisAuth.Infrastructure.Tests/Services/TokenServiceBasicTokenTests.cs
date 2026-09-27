using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Infrastructure.Helpers;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Basic token (applicationId:secret) verification with application secrets.
/// </summary>
public class TokenServiceBasicTokenTests
{
	#region Constants
	
	private const string ApplicationId = "6a7b8c9d0e1f2a3b4c5d6e7f";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IApplicationService _applicationService = Substitute.For<IApplicationService>();
	
	private readonly Membership _membership;
	
	private readonly Application _application;
	
	private readonly string _secret = ApplicationSecretHelper.GenerateSecret();
	
	#endregion
	
	#region Constructors
	
	public TokenServiceBasicTokenTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		
		this._application = new Application
		{
			Id = ApplicationId,
			Name = "server-app",
			Role = "server",
			MembershipId = this._membership.Id,
			SecretHash = ApplicationSecretHelper.HashSecret(this._secret)
		};
		
		this._applicationService.GetByIdAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns(this._application);
	}
	
	#endregion
	
	#region Helpers
	
	private TokenService CreateTokenService()
	{
		return new TokenService(
			this._membershipService,
			Substitute.For<IUserService>(),
			this._applicationService,
			Substitute.For<IRoleService>(),
			new JwtService(),
			Substitute.For<IEventService>(),
			Substitute.For<IActiveTokenService>(),
			Substitute.For<IRevokedTokenService>(),
			TestServiceFactory.CreateLegacyApplicationSecretVerifier(),
			NullLogger<TokenService>.Instance);
	}
	
	private async Task<ErtisAuthException> AssertInvalidTokenAsync(string basicToken)
	{
		var tokenService = this.CreateTokenService();
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync(basicToken, false, cancellationToken: TestContext.Current.CancellationToken));
		Assert.Equal("InvalidToken", exception.ErrorCode);
		return exception;
	}
	
	#endregion
	
	#region Verify Basic Token
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WithApplicationSecret_ReturnsApplication()
	{
		var tokenService = this.CreateTokenService();
		
		var result = await tokenService.VerifyBasicTokenAsync($"{ApplicationId}:{this._secret}", false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(result.IsValidated);
		Assert.Equal(ApplicationId, result.Application.Id);
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WithMembershipSecretForApplicationWithOwnSecret_ThrowsInvalidToken()
	{
		// Even while the legacy switch allows the membership secret, an application with its own secret only accepts that
		this._membership.AllowMembershipSecretForApplications = true;
		
		await this.AssertInvalidTokenAsync($"{ApplicationId}:{this._membership.SecretKey}");
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WithPreviousSecretAfterRotation_ThrowsInvalidToken()
	{
		var previousSecret = this._secret;
		this._application.SecretHash = ApplicationSecretHelper.HashSecret(ApplicationSecretHelper.GenerateSecret());
		
		await this.AssertInvalidTokenAsync($"{ApplicationId}:{previousSecret}");
	}
	
	[Theory]
	[InlineData("wrong-secret")]
	[InlineData("")]
	public async Task VerifyBasicTokenAsync_WithWrongSecret_ThrowsInvalidToken(string secret)
	{
		await this.AssertInvalidTokenAsync($"{ApplicationId}:{secret}");
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WithSecretDifferingOnlyInLastCharacter_ThrowsInvalidToken()
	{
		var almostSecret = this._secret[..^1] + (this._secret[^1] == 'x' ? 'y' : 'x');
		
		await this.AssertInvalidTokenAsync($"{ApplicationId}:{almostSecret}");
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WithUnknownApplication_ThrowsSameErrorAsWrongSecret()
	{
		var unknownApplication = await this.AssertInvalidTokenAsync($"unknown-application-id:{this._secret}");
		var wrongSecret = await this.AssertInvalidTokenAsync($"{ApplicationId}:wrong-secret");
		
		Assert.Equal(wrongSecret.StatusCode, unknownApplication.StatusCode);
		Assert.Equal(wrongSecret.Message, unknownApplication.Message);
	}
	
	[Fact]
	public async Task VerifyBasicTokenAsync_WithApplicationOfUnknownMembership_ThrowsInvalidToken()
	{
		this._application.MembershipId = "unknown-membership-id";
		
		await this.AssertInvalidTokenAsync($"{ApplicationId}:{this._secret}");
	}
	
	[Theory]
	[InlineData("")]
	[InlineData("no-separator")]
	[InlineData("too:many:parts")]
	public async Task VerifyBasicTokenAsync_WithMalformedToken_ThrowsInvalidToken(string basicToken)
	{
		await this.AssertInvalidTokenAsync(basicToken);
	}
	
	#endregion
}
