using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Security expectations of the token lifecycle, using the real JwtService.
/// </summary>
public class TokenServiceSecurityTests
{
	#region Constants
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d0e";
	
	private const string AttackerSecretKey = "attacker-secret-key-attacker-secret-key-attacker";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly IActiveTokenService _activeTokenService = Substitute.For<IActiveTokenService>();
	
	private readonly IRevokedTokenService _revokedTokenService = Substitute.For<IRevokedTokenService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly JwtService _jwtService = new();
	
	#endregion
	
	#region Helpers
	
	private TokenService CreateTokenService()
	{
		return new TokenService(
			this._membershipService,
			this._userService,
			Substitute.For<IApplicationService>(),
			this._roleService,
			this._jwtService,
			Substitute.For<IEventService>(),
			this._activeTokenService,
			this._revokedTokenService,
			NullLogger<TokenService>.Instance);
	}
	
	private (Membership Membership, User User) Setup(bool isActive = true, string? defaultEncoding = null)
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256", defaultEncoding);
		this._membershipService.GetAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);
		
		var user = new UserWithPasswordHash
		{
			Id = UserId,
			Username = "john.doe",
			EmailAddress = "john.doe@example.com",
			Role = "user",
			IsActive = isActive,
			MembershipId = membership.Id,
			PasswordHash = "0e44ce7308af2b3de5232e4616403ce7d49ba2aec83f79c196409556422a4927"
		};
		
		this._userService.GetUserAsync(membership.Id, UserId, Arg.Any<CancellationToken>()).Returns(user);
		this._userService.GetUserWithPasswordAsync(membership.Id, user.Username, user.Username, Arg.Any<CancellationToken>()).Returns(user);
		this._userService.VerifyPassword(membership, "P@ssw0rd!", user.PasswordHash).Returns(true);
		return (membership, user);
	}
	
	private void SetupRole(Membership membership, User user, params string[] permissions)
	{
		this._roleService.GetBySlugAsync(user.Role, membership.Id, Arg.Any<CancellationToken>()).Returns(new Role
		{
			Id = "role-id",
			Name = user.Role,
			MembershipId = membership.Id,
			Permissions = permissions
		});
	}
	
	private async Task<BearerToken> GenerateTokenAsync(TokenService tokenService, Membership membership)
	{
		return await tokenService.GenerateTokenAsync("john.doe", "P@ssw0rd!", membership.Id, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	/// <summary>
	/// Builds a token with valid-looking claims (issuer, audience, subject, membership) but signed with a key the attacker controls.
	/// </summary>
	private string ForgeToken(Membership membership, User user, bool isRefreshToken, string signingSecretKey = AttackerSecretKey)
	{
		var forgedMembership = TestServiceFactory.CreateMembership("SHA2-256");
		forgedMembership.Id = membership.Id;
		forgedMembership.Name = membership.Name;
		forgedMembership.SecretKey = signingSecretKey;
		
		var claims = new TokenClaims(Guid.NewGuid().ToString(), user, forgedMembership);
		if (isRefreshToken)
		{
			claims.AddClaim("refresh_token", true);
		}
		
		return this._jwtService.GenerateToken(claims, expiresIn: TimeSpan.FromHours(1));
	}
	
	#endregion
	
	#region Forged Tokens
	
	[Fact]
	public async Task RefreshTokenAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var (membership, user) = this.Setup();
		var forgedRefreshToken = this.ForgeToken(membership, user, isRefreshToken: true);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.RefreshTokenAsync(forgedRefreshToken, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var (membership, user) = this.Setup();
		var forgedToken = this.ForgeToken(membership, user, isRefreshToken: false);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(forgedToken, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithForgedTokenOfInactiveUser_DoesNotRevealAccountStatus()
	{
		var (membership, user) = this.Setup(isActive: false);
		var forgedToken = this.ForgeToken(membership, user, isRefreshToken: false);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(forgedToken, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
		await this._userService.DidNotReceiveWithAnyArgs().GetUserAsync(default!, default!, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Genuine Tokens
	
	[Theory]
	[InlineData(null)]
	[InlineData("utf-8")]
	[InlineData("utf-16")]
	public async Task VerifyBearerTokenAsync_WithIssuedAccessToken_IsValidated(string? defaultEncoding)
	{
		var (membership, _) = this.Setup(defaultEncoding: defaultEncoding);
		var tokenService = this.CreateTokenService();
		var token = await tokenService.GenerateTokenAsync("john.doe", "P@ssw0rd!", membership.Id, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		var result = await tokenService.VerifyBearerTokenAsync(token.AccessToken, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(result.IsValidated);
		Assert.False(result.IsRefreshToken);
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithIssuedRefreshToken_ReturnsNewToken()
	{
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var refreshed = await tokenService.RefreshTokenAsync(token.RefreshToken!, revokeBefore: false, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.NotEqual(token.AccessToken, refreshed.AccessToken);
		var result = await tokenService.VerifyBearerTokenAsync(refreshed.AccessToken, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(result.IsValidated);
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithAccessToken_ThrowsTokenIsNotRefreshable()
	{
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.RefreshTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenIsNotRefreshable", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithIssuedRefreshToken_IsValidatedAndReportedAsRefreshToken()
	{
		// The verify endpoint still introspects refresh tokens (token_kind: refresh_token); they are rejected where a token is used as an access token.
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var result = await tokenService.VerifyBearerTokenAsync(token.RefreshToken!, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(result.IsValidated);
		Assert.True(result.IsRefreshToken);
		Assert.Equal("refresh_token", result.TokenKind);
	}
	
	[Fact]
	public async Task WhoAmIAsync_WithRefreshToken_ThrowsInvalidToken()
	{
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		var refreshTokenAsBearer = new BearerToken(token.RefreshToken!, token.RefreshExpiresIn, null, TimeSpan.Zero);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.WhoAmIAsync(refreshTokenAsBearer, TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public async Task WhoAmIAsync_WithAccessToken_ReturnsTokenOwner()
	{
		var (membership, user) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var owner = await tokenService.WhoAmIAsync(token, TestContext.Current.CancellationToken);
		
		Assert.Equal(user.Id, owner?.Id);
	}
	
	[Fact]
	public async Task GenerateScopedTokenAsync_WithRefreshToken_ThrowsInvalidToken()
	{
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(token.RefreshToken!, ["users.read"], membership.Id, TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithExpiredToken_ThrowsTokenWasExpired()
	{
		var (membership, user) = this.Setup();
		var tokenService = this.CreateTokenService();
		var claims = new TokenClaims(Guid.NewGuid().ToString(), user, membership);
		var expiredToken = this._jwtService.GenerateToken(claims, generationTime: DateTime.UtcNow.AddHours(-2), expiresIn: TimeSpan.FromHours(1));
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(expiredToken, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenWasExpired", exception.ErrorCode);
	}
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithRevokedToken_ThrowsTokenWasRevoked()
	{
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		this._revokedTokenService.GetByAccessTokenAsync(token.AccessToken, Arg.Any<CancellationToken>()).Returns(new RevokedToken
		{
			Id = "revoked-token-id",
			Token = token.AccessToken,
			MembershipId = membership.Id
		});
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenWasRevoked", exception.ErrorCode);
	}
	
	
	[Fact]
	public async Task GenerateScopedTokenAsync_WithScopeExplicitlyPermittedByUbac_ReturnsScopedToken()
	{
		var (membership, user) = this.Setup();
		user.Permissions = ["users.read"];
		this._roleService.GetBySlugAsync(user.Role, membership.Id, Arg.Any<CancellationToken>()).Returns(new Role
		{
			Id = "role-id",
			Name = user.Role,
			MembershipId = membership.Id,
			Permissions = ["users.*"]
		});
		
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var scopedToken = await tokenService.GenerateTokenAsync(token.AccessToken, ["users.read"], membership.Id, TestContext.Current.CancellationToken);
		
		Assert.NotNull(scopedToken.AccessToken);
	}
	
	
	#endregion
	
	#region Scoped Tokens
	
	[Fact]
	public async Task RefreshTokenAsync_WithStoredRefreshTokenOfScopedToken_KeepsTheScopes()
	{
		var (membership, user) = this.Setup();
		this.SetupRole(membership, user, "*");
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		// Scoped tokens are returned without their refresh token, but one is still issued and stored with the active token.
		BearerToken? issuedScopedToken = null;
		await this._activeTokenService.CreateAsync(Arg.Do<BearerToken>(x => issuedScopedToken = x), Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
		await tokenService.GenerateTokenAsync(token.AccessToken, ["users.read", "roles.read"], membership.Id, TestContext.Current.CancellationToken);
		Assert.NotNull(issuedScopedToken?.RefreshToken);
		
		var refreshed = await tokenService.RefreshTokenAsync(issuedScopedToken.RefreshToken, revokeBefore: false, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		var result = await tokenService.VerifyBearerTokenAsync(refreshed.AccessToken, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		Assert.NotNull(result.Scopes);
		Assert.Equal(["users.read", "roles.read"], result.Scopes);
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithRefreshTokenOfUnscopedToken_ReturnsUnscopedToken()
	{
		var (membership, _) = this.Setup();
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		
		var refreshed = await tokenService.RefreshTokenAsync(token.RefreshToken!, revokeBefore: false, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		var result = await tokenService.VerifyBearerTokenAsync(refreshed.AccessToken, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Null(result.Scopes);
	}
	
	[Theory]
	[InlineData("users.*")]
	[InlineData("roles.read")]
	[InlineData("*")]
	public async Task GenerateScopedTokenAsync_WithScopedTokenRequestingBroaderScope_IsRejected(string requestedScope)
	{
		var (membership, user) = this.Setup();
		this.SetupRole(membership, user, "*");
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		var scopedToken = await tokenService.GenerateTokenAsync(token.AccessToken, ["users.read"], membership.Id, TestContext.Current.CancellationToken);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.GenerateTokenAsync(scopedToken.AccessToken, [requestedScope], membership.Id, TestContext.Current.CancellationToken));
		
		Assert.Equal("UserHasNoPermissionForThisScope", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData("users.read")]
	[InlineData("users.read.some-user-id")]
	[InlineData("*.users.read.*")]
	public async Task GenerateScopedTokenAsync_WithScopedTokenRequestingNarrowerOrEqualScope_Succeeds(string requestedScope)
	{
		var (membership, user) = this.Setup();
		this.SetupRole(membership, user, "*");
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService, membership);
		var scopedToken = await tokenService.GenerateTokenAsync(token.AccessToken, ["users.read"], membership.Id, TestContext.Current.CancellationToken);
		
		var narrowedToken = await tokenService.GenerateTokenAsync(scopedToken.AccessToken, [requestedScope], membership.Id, TestContext.Current.CancellationToken);
		
		Assert.NotNull(narrowedToken.AccessToken);
	}
	
	#endregion
	
	#region Membership Isolation
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithUnknownMembership_ThrowsInvalidTokenInsteadOfMembershipNotFound()
	{
		var (_, user) = this.Setup();
		var unknownMembership = TestServiceFactory.CreateMembership("SHA2-256");
		unknownMembership.Id = "unknown-membership-id";
		var claims = new TokenClaims(Guid.NewGuid().ToString(), user, unknownMembership);
		var token = this._jwtService.GenerateToken(claims);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(token, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
		await this._userService.DidNotReceiveWithAnyArgs().GetUserAsync(default!, default!, TestContext.Current.CancellationToken);
	}
	
	[Fact]
	public async Task VerifyBearerTokenAsync_WithTokenSignedByAnotherMembershipKey_ThrowsInvalidToken()
	{
		// Someone who knows membership B's secret must not be able to mint tokens that membership A accepts.
		var (membershipA, user) = this.Setup();
		var membershipB = TestServiceFactory.CreateMembership("SHA2-256");
		membershipB.Id = "membership-b";
		membershipB.SecretKey = "membership-b-secret-key-membership-b-secret-key";
		this._membershipService.GetAsync(membershipB.Id, Arg.Any<CancellationToken>()).Returns(membershipB);
		var tokenClaimingA = this.ForgeToken(membershipA, user, isRefreshToken: false, signingSecretKey: membershipB.SecretKey);
		var tokenService = this.CreateTokenService();
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(tokenClaimingA, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	#endregion
}
