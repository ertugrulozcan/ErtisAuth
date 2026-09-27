using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Basic token (applicationId:secret) verification.
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
			MembershipId = this._membership.Id
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
			NullLogger<TokenService>.Instance);
	}

	#endregion

	#region Verify Basic Token

	[Fact]
	public async Task VerifyBasicTokenAsync_WithMembershipSecret_ReturnsApplication()
	{
		var tokenService = this.CreateTokenService();

		var result = await tokenService.VerifyBasicTokenAsync($"{ApplicationId}:{this._membership.SecretKey}", false, cancellationToken: TestContext.Current.CancellationToken);

		Assert.True(result.IsValidated);
		Assert.Equal(ApplicationId, result.Application.Id);
	}

	[Theory]
	[InlineData("wrong-secret")]
	[InlineData("")]
	public async Task VerifyBasicTokenAsync_WithWrongSecret_ThrowsInvalidToken(string secret)
	{
		var tokenService = this.CreateTokenService();

		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync($"{ApplicationId}:{secret}", false, cancellationToken: TestContext.Current.CancellationToken));

		Assert.Equal("InvalidToken", exception.ErrorCode);
	}

	[Fact]
	public async Task VerifyBasicTokenAsync_WithSecretDifferingOnlyInLastCharacter_ThrowsInvalidToken()
	{
		var tokenService = this.CreateTokenService();
		var secretKey = this._membership.SecretKey;
		var almostSecret = secretKey[..^1] + (secretKey[^1] == 'x' ? 'y' : 'x');

		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync($"{ApplicationId}:{almostSecret}", false, cancellationToken: TestContext.Current.CancellationToken));

		Assert.Equal("InvalidToken", exception.ErrorCode);
	}

	[Fact]
	public async Task VerifyBasicTokenAsync_WithUnknownApplication_ThrowsSameErrorAsWrongSecret()
	{
		var tokenService = this.CreateTokenService();

		var unknownApplication = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync($"unknown-application-id:{this._membership.SecretKey}", false, cancellationToken: TestContext.Current.CancellationToken));
		var wrongSecret = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync($"{ApplicationId}:wrong-secret", false, cancellationToken: TestContext.Current.CancellationToken));

		Assert.Equal(wrongSecret.ErrorCode, unknownApplication.ErrorCode);
		Assert.Equal(wrongSecret.StatusCode, unknownApplication.StatusCode);
		Assert.Equal(wrongSecret.Message, unknownApplication.Message);
	}

	[Fact]
	public async Task VerifyBasicTokenAsync_WithApplicationOfUnknownMembership_ThrowsInvalidToken()
	{
		this._application.MembershipId = "unknown-membership-id";
		var tokenService = this.CreateTokenService();

		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync($"{ApplicationId}:{this._membership.SecretKey}", false, cancellationToken: TestContext.Current.CancellationToken));

		Assert.Equal("InvalidToken", exception.ErrorCode);
	}

	[Theory]
	[InlineData("")]
	[InlineData("no-separator")]
	[InlineData("too:many:parts")]
	public async Task VerifyBasicTokenAsync_WithMalformedToken_ThrowsInvalidToken(string basicToken)
	{
		var tokenService = this.CreateTokenService();

		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBasicTokenAsync(basicToken, false, cancellationToken: TestContext.Current.CancellationToken));

		Assert.Equal("InvalidToken", exception.ErrorCode);
	}

	#endregion
}
