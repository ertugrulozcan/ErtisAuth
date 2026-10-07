using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Revoking a token revokes both sides of its access/refresh token pair, using the real JwtService
/// and in-memory active/revoked token stores.
/// </summary>
public class TokenServiceRevokeTests
{
	#region Constants
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d0e";
	
	private const string Password = "P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IUserService _userService = Substitute.For<IUserService>();
	
	private readonly IActiveTokenService _activeTokenService = Substitute.For<IActiveTokenService>();
	
	private readonly IRevokedTokenService _revokedTokenService = Substitute.For<IRevokedTokenService>();
	
	private readonly List<ActiveToken> _activeTokens = [];
	
	private readonly Dictionary<string, RevokedToken> _revokedTokens = new();
	
	private readonly Membership _membership;
	
	private readonly User _user;
	
	#endregion
	
	#region Constructors
	
	public TokenServiceRevokeTests()
	{
		this._membership = TestServiceFactory.CreateMembership("SHA2-256");
		this._membershipService.GetAsync(this._membership.Id, Arg.Any<CancellationToken>()).Returns(this._membership);
		
		var user = new UserWithPasswordHash
		{
			Id = UserId,
			Username = "john.doe",
			EmailAddress = "john.doe@example.com",
			Role = "user",
			IsActive = true,
			MembershipId = this._membership.Id,
			PasswordHash = "0e44ce7308af2b3de5232e4616403ce7d49ba2aec83f79c196409556422a4927"
		};
		
		this._user = user;
		this._userService.GetUserAsync(UserId, this._membership.Id, Arg.Any<CancellationToken>()).Returns(user);
		this._userService.GetUserWithPasswordAsync(user.Username, user.Username, this._membership.Id, Arg.Any<CancellationToken>()).Returns(user);
		this._userService.VerifyPassword(Password, user.PasswordHash, this._membership).Returns(true);
		
		this._activeTokenService
			.CreateAsync(Arg.Any<BearerToken>(), Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var bearerToken = callInfo.ArgAt<BearerToken>(0);
				var activeToken = new ActiveToken
				{
					Id = Guid.NewGuid().ToString(),
					AccessToken = bearerToken.AccessToken,
					RefreshToken = bearerToken.RefreshToken,
					ExpiresIn = bearerToken.ExpiresInTimeStamp,
					RefreshTokenExpiresIn = bearerToken.RefreshTokenExpiresInTimeStamp,
					CreatedAt = bearerToken.CreatedAt,
					UserId = callInfo.ArgAt<User>(1).Id,
					MembershipId = callInfo.ArgAt<string>(2)
				};
				
				this._activeTokens.Add(activeToken);
				return activeToken;
			});
		
		this._activeTokenService
			.GetActiveTokensByUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this._activeTokens.Where(x => x.UserId == callInfo.ArgAt<string>(0) && x.MembershipId == callInfo.ArgAt<string>(1)).ToArray());
		
		this._activeTokenService
			.When(x => x.BulkDeleteAsync(Arg.Any<IEnumerable<ActiveToken>>(), Arg.Any<CancellationToken>()))
			.Do(callInfo =>
			{
				var deletedTokens = callInfo.ArgAt<IEnumerable<ActiveToken>>(0).ToArray();
				this._activeTokens.RemoveAll(x => deletedTokens.Contains(x));
			});
		
		this._revokedTokenService
			.When(x => x.RevokeAsync(Arg.Any<string>(), Arg.Any<User>(), Arg.Any<bool>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
			.Do(callInfo =>
			{
				var token = callInfo.ArgAt<string>(0);
				this._revokedTokens[token] = new RevokedToken
				{
					Id = Guid.NewGuid().ToString(),
					Token = token,
					MembershipId = callInfo.ArgAt<User>(1).MembershipId,
					TokenType = callInfo.ArgAt<bool>(2) ? "refresh_token" : "bearer_token",
					RetainUntil = callInfo.ArgAt<DateTime>(3)
				};
			});
		
		this._revokedTokenService
			.GetByAccessTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => this._revokedTokens.GetValueOrDefault(callInfo.ArgAt<string>(0)));
	}
	
	#endregion
	
	#region Helpers
	
	private TokenService CreateTokenService()
	{
		return new TokenService(
			this._membershipService,
			this._userService,
			Substitute.For<IApplicationService>(),
			Substitute.For<IRoleService>(),
			new JwtService(),
			Substitute.For<IEventService>(),
			this._activeTokenService,
			this._revokedTokenService,
			TestServiceFactory.CreateLegacyApplicationSecretVerifier(),
			NullLogger<TokenService>.Instance);
	}
	
	private async Task<BearerToken> GenerateTokenAsync(TokenService tokenService)
	{
		return await tokenService.GenerateTokenAsync(this._user.Username, Password, this._membership.Id, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
	}
	
	private void AssertRevoked(BearerToken token)
	{
		Assert.Equal("bearer_token", this._revokedTokens[token.AccessToken].TokenType);
		Assert.Equal("refresh_token", this._revokedTokens[token.RefreshToken!].TokenType);
		Assert.DoesNotContain(this._activeTokens, x => x.AccessToken == token.AccessToken);
	}
	
	private void AssertNotRevoked(BearerToken token)
	{
		Assert.False(this._revokedTokens.ContainsKey(token.AccessToken));
		Assert.False(this._revokedTokens.ContainsKey(token.RefreshToken!));
		Assert.Contains(this._activeTokens, x => x.AccessToken == token.AccessToken);
	}
	
	#endregion
	
	#region Revoke Token
	
	[Fact]
	public async Task RevokeTokenAsync_WithAccessToken_RevokesItsRefreshTokenToo()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		
		var isRevoked = await tokenService.RevokeTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(isRevoked);
		this.AssertRevoked(token);
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.RefreshTokenAsync(token.RefreshToken!, cancellationToken: TestContext.Current.CancellationToken));
		Assert.Equal("RefreshTokenWasRevoked", exception.ErrorCode);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_WithRefreshToken_RevokesItsAccessTokenToo()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		
		var isRevoked = await tokenService.RevokeTokenAsync(token.RefreshToken!, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(isRevoked);
		this.AssertRevoked(token);
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.VerifyBearerTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken));
		Assert.Equal("TokenWasRevoked", exception.ErrorCode);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_WithoutLogoutFromAllDevices_LeavesOtherSessionsActive()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		var otherToken = await this.GenerateTokenAsync(tokenService);
		
		await tokenService.RevokeTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken);
		
		this.AssertRevoked(token);
		this.AssertNotRevoked(otherToken);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_WithLogoutFromAllDevices_RevokesAllPairsOfUser()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		var otherToken = await this.GenerateTokenAsync(tokenService);
		
		await tokenService.RevokeTokenAsync(token.AccessToken, logoutFromAllDevices: true, cancellationToken: TestContext.Current.CancellationToken);
		
		this.AssertRevoked(token);
		this.AssertRevoked(otherToken);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_WithAlreadyRevokedToken_ReturnsFalse()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		await tokenService.RevokeTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken);
		
		var isRevoked = await tokenService.RevokeTokenAsync(token.RefreshToken!, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.False(isRevoked);
	}
	
	[Fact]
	public async Task RevokeAllAsync_RevokesAllPairsOfUser()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		var otherToken = await this.GenerateTokenAsync(tokenService);
		
		await tokenService.RevokeAllAsync(UserId, this._membership.Id, cancellationToken: TestContext.Current.CancellationToken);
		
		this.AssertRevoked(token);
		this.AssertRevoked(otherToken);
	}
	
	#endregion
	
	#region Retention
	
	[Fact]
	public async Task RevokeTokenAsync_KeepsEachRevocationUntilItsTokenExpires()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		var activeToken = this._activeTokens.Single(x => x.AccessToken == token.AccessToken);
		
		await tokenService.RevokeTokenAsync(token.AccessToken, cancellationToken: TestContext.Current.CancellationToken);
		
		// The test membership: access tokens live 1 hour, refresh tokens 2 hours
		Assert.Equal(activeToken.CreatedAt.AddSeconds(3600), this._revokedTokens[token.AccessToken].RetainUntil);
		Assert.Equal(activeToken.CreatedAt.AddSeconds(7200), this._revokedTokens[token.RefreshToken!].RetainUntil);
	}
	
	#endregion
	
	#region Refresh Token
	
	[Fact]
	public async Task RefreshTokenAsync_WithRevokeBefore_RevokesOriginalPairAndBlocksReplay()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		
		var refreshedToken = await tokenService.RefreshTokenAsync(token.RefreshToken!, revokeBefore: true, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		this.AssertRevoked(token);
		this.AssertNotRevoked(refreshedToken);
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => tokenService.RefreshTokenAsync(token.RefreshToken!, cancellationToken: TestContext.Current.CancellationToken));
		Assert.Equal("RefreshTokenWasRevoked", exception.ErrorCode);
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithoutRevokeBefore_KeepsOriginalPairActive()
	{
		var tokenService = this.CreateTokenService();
		var token = await this.GenerateTokenAsync(tokenService);
		
		var refreshedToken = await tokenService.RefreshTokenAsync(token.RefreshToken!, revokeBefore: false, fireEvent: false, cancellationToken: TestContext.Current.CancellationToken);
		
		this.AssertNotRevoked(token);
		this.AssertNotRevoked(refreshedToken);
	}
	
	#endregion
}
