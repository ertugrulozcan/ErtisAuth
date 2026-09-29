using System.Net;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Infrastructure.Extensions;
using ErtisAuth.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ErtisAuth.Infrastructure.Services;

public class TokenService : ITokenService
{
	#region Constants
	
	private const string REFRESH_TOKEN_CLAIM = "refresh_token";
	
	#endregion
	
	#region Services
	
	private readonly IMembershipService _membershipService;
	private readonly IUserService _userService;
	private readonly IApplicationService _applicationService;
	private readonly IRoleService _roleService;
	private readonly IJwtService _jwtService;
	private readonly IEventService _eventService;
	private readonly IActiveTokenService _activeTokenService;
	private readonly IRevokedTokenService _revokedTokenService;
	private readonly LegacyApplicationSecretVerifier _legacyApplicationSecretVerifier; // LEGACY-APP-SECRET
	private readonly ILogger<TokenService> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="userService"></param>
	/// <param name="applicationService"></param>
	/// <param name="roleService"></param>
	/// <param name="jwtService"></param>
	/// <param name="eventService"></param>
	/// <param name="activeTokenService"></param>
	/// <param name="revokedTokenService"></param>
	/// <param name="legacyApplicationSecretVerifier"></param>
	/// <param name="logger"></param>
	public TokenService(
		IMembershipService membershipService, 
		IUserService userService, 
		IApplicationService applicationService,
		IRoleService roleService,
		IJwtService jwtService,
		IEventService eventService,
		IActiveTokenService activeTokenService,
		IRevokedTokenService revokedTokenService,
		LegacyApplicationSecretVerifier legacyApplicationSecretVerifier, // LEGACY-APP-SECRET
		ILogger<TokenService> logger)
	{
		this._membershipService = membershipService;
		this._userService = userService;
		this._applicationService = applicationService;
		this._roleService = roleService;
		this._jwtService = jwtService;
		this._eventService = eventService;
		this._activeTokenService = activeTokenService;
		this._revokedTokenService = revokedTokenService;
		this._legacyApplicationSecretVerifier = legacyApplicationSecretVerifier; // LEGACY-APP-SECRET
		this._logger = logger;
	}
	
	#endregion
	
	#region WhoAmI
	
	public async Task<User?> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default)
	{
		var verifyResult = await this.VerifyBearerTokenAsync(bearerToken.AccessToken, false, cancellationToken: cancellationToken);
		if (verifyResult.IsRefreshToken)
		{
			throw ErtisAuthException.InvalidToken("Refresh tokens can not be used as access tokens");
		}

		return await this.GetTokenOwnerUserAsync(bearerToken.AccessToken, cancellationToken: cancellationToken);
	}
	
	public async Task<User?> GetTokenOwnerUserAsync(string bearerToken, CancellationToken cancellationToken = default)
	{
		if (this._jwtService.TryDecodeToken(bearerToken, out var securityToken) && securityToken != null)
		{
			return await this.GetTokenOwnerAsync(securityToken, cancellationToken: cancellationToken);
		}
		else
		{
			return null;
		}
	}
	
	public async Task<Application?> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default)
	{
		await this.VerifyBasicTokenAsync(basicToken.AccessToken, false, cancellationToken: cancellationToken);
		return await this.GetTokenOwnerApplicationAsync(basicToken.AccessToken, cancellationToken: cancellationToken);
	}
	
	private async Task<Application> GetTokenOwnerApplicationAsync(string basicToken, CancellationToken cancellationToken = default)
	{
		var parts = basicToken.Split(':');
		if (parts.Length != 2)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var applicationId = parts[0];
		var application = await this._applicationService.GetByIdAsync(applicationId, cancellationToken: cancellationToken);
		if (application == null)
		{
			throw ErtisAuthException.ApplicationNotFound(applicationId);
		}
		
		return application;
	}
	
	private async Task<User?> GetTokenOwnerAsync(JsonWebToken securityToken, CancellationToken cancellationToken = default)
	{
		if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
		{
			var userId = securityToken.Subject;
			if (!string.IsNullOrEmpty(userId))
			{
				return await this._userService.GetUserAsync(membershipId, userId, cancellationToken: cancellationToken);
			}
			else
			{
				// UserId could not find in token claims!
				throw ErtisAuthException.InvalidToken();
			}
		}
		else
		{
			// MembershipId could not find in token claims!
			throw ErtisAuthException.InvalidToken();
		}
	}
	
	#endregion
	
	#region Generate Token
	
	public async Task<BearerToken> GenerateTokenAsync(
		string username, 
		string password, 
		string membershipId, 
		string? ipAddress = null, 
		string? userAgent = null, 
		bool fireEvent = true, 
		CancellationToken cancellationToken = default)
	{
		// Check membership
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		// Check user
		var user = await this._userService.GetUserWithPasswordAsync(membership.Id, username, username, cancellationToken: cancellationToken);
		if (user == null)
		{
			// Spend a comparable hashing cost so that response times do not reveal whether the user exists
			this._userService.CalculatePasswordHash(membership, password);
			throw ErtisAuthException.InvalidCredentials();
		}

		// Check password before the account status, so that the status is not revealed to callers without valid credentials
		if (!this._userService.VerifyPassword(membership, password, user.PasswordHash))
		{
			throw ErtisAuthException.InvalidCredentials();
		}

		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}

		return await this.GenerateBearerTokenAsync(user, membership, null, ipAddress, userAgent, cancellationToken: cancellationToken);
	}
	
	public async Task<ScopedBearerToken> GenerateTokenAsync(
		string token, 
		string[]? scopes, 
		string membershipId,
		CancellationToken cancellationToken = default)
	{
		if (scopes == null || scopes.Length == 0)
		{
			throw ErtisAuthException.ScopeRequired();
		}
		
		var verifyResult = await this.VerifyBearerTokenAsync(token, cancellationToken: cancellationToken);
		if (verifyResult.IsRefreshToken)
		{
			throw ErtisAuthException.InvalidToken("Refresh tokens can not be used as access tokens");
		}
		
		if (verifyResult is { IsValidated: true, User: not null })
		{
			if (membershipId != verifyResult.User.MembershipId)
			{
				throw ErtisAuthException.Synthetic(HttpStatusCode.BadRequest, "Membership ids do not match", "MembershipIdsDoNotMatch");
			}
			
			var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}
			
			var role = await this._roleService.GetBySlugAsync(verifyResult.User.Role, membershipId, cancellationToken: cancellationToken);
			if (role != null)
			{
				foreach (var scope in scopes)
				{
					if (Rbac.TryParse(scope, out var rbac))
					{
						// A token can only be narrowed: with an already scoped token, the requested scope must be covered by its scopes
						if (verifyResult.Scopes is { Length: > 0 } && !verifyResult.Scopes.CoversScope(rbac!))
						{
							throw ErtisAuthException.UserHasNoPermissionForThisScope(scope);
						}
						
						// Same precedence as AccessControlService: a matching UBAC entry is decisive, otherwise the role decides
						var isPermitted = verifyResult.User.HasPermission(rbac!) ?? role.HasPermission(rbac!);
						if (!isPermitted)
						{
							throw ErtisAuthException.UserHasNoPermissionForThisScope(scope);
						}
					}
					else
					{
						throw ErtisAuthException.InvalidScope(scope);
					}
				}
				
				var bearerToken = await this.GenerateBearerTokenAsync(verifyResult.User, membership, scopes, cancellationToken: cancellationToken);
				return new ScopedBearerToken(bearerToken, scopes);
			}
			else
			{
				throw ErtisAuthException.RoleNotFound(verifyResult.User.Role);
			}
		}
		else
		{
			throw ErtisAuthException.Unauthorized("Token was not verified");
		}
	}
	
	public async Task<BearerToken> GenerateTokenAsync(User user, string membershipId, string? ipAddress = null, string? userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		// Check membership
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		// Check user
		var currentUser = await this._userService.GetUserAsync(membership.Id, user.Id, cancellationToken: cancellationToken);
		if (currentUser == null)
		{
			throw ErtisAuthException.UserNotFound(user.Id, "id");
		}
		
		return await this.GenerateBearerTokenAsync(currentUser, membership, null, ipAddress, userAgent, cancellationToken: cancellationToken);
	}
	
	private async Task<BearerToken> GenerateBearerTokenAsync(
		User user, 
		Membership membership, 
		string[]? scopes, 
		string? ipAddress = null, 
		string? userAgent = null, 
		bool fireEvent = true, 
		CancellationToken cancellationToken = default)
	{
		var tokenId = Guid.NewGuid().ToString();
		TimeSpan? expiresIn = scopes is { Length: > 0 }
			? membership.ScopedTokenExpiresIn == 0
				? TTLs.SCOPED_TOKEN_TTL
				: TimeSpan.FromSeconds(membership.ScopedTokenExpiresIn)
			: null;
		
		var tokenClaims = new TokenClaims(tokenId, user, membership, expiresIn)
		{
			Scope = scopes is { Length: > 0 } ? string.Join(" ", scopes) : null
		};
		
		var encoding = membership.GetEncoding();
		var accessToken = this._jwtService.GenerateToken(tokenClaims, encoding: encoding);
		var refreshExpiresIn = TimeSpan.FromSeconds(membership.RefreshTokenExpiresIn);
		var refreshToken = this._jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), expiresIn: refreshExpiresIn, encoding: encoding);
		var bearerToken = new BearerToken(accessToken, tokenClaims.ExpiresIn, refreshToken, refreshExpiresIn);
		
		// Save to active tokens collection
		await this._activeTokenService.CreateAsync(bearerToken, user, membership.Id, ipAddress, userAgent, cancellationToken: cancellationToken);
		
		if (fireEvent)
		{
			// Events are readable (events.read) and forwarded to webhooks: only token metadata, never the token itself
			var tokenInfo = new
			{
				token_type = "bearer",
				expires_in = bearerToken.ExpiresInTimeStamp,
				refresh_token_expires_in = bearerToken.RefreshTokenExpiresInTimeStamp,
				created_at = bearerToken.CreatedAt
			};
			
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenGenerated, user, membership.Id, new { user, token = tokenInfo }, cancellationToken: cancellationToken);
		}
		
		return bearerToken;
	}
	
	private bool IsRefreshToken(JsonWebToken securityToken)
	{
		var refreshTokenClaim = securityToken.Claims.FirstOrDefault(x => x.Type == REFRESH_TOKEN_CLAIM);
		return 
			refreshTokenClaim != null && 
			bool.TryParse(refreshTokenClaim.Value, out var isRefreshableToken) && 
			isRefreshableToken;
	}
	
	private string[]? ExtractScopes(JsonWebToken securityToken)
	{
		if (this.TryExtractClaimValue(securityToken, "scope", out var scopeClaim) && !string.IsNullOrEmpty(scopeClaim))
		{
			var scopes = scopeClaim.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
			return scopes.Length > 0 ? scopes : null;
		}
		
		return null;
	}
	
	private bool TryExtractClaimValue(JsonWebToken securityToken, string key, out string? value)
	{
		var claim = securityToken.Claims.FirstOrDefault(x => x.Type == key);
		if (claim != null)
		{
			value = claim.Value;
			return true;
		}
		else
		{
			value = null;
			return false;
		}
	}
	
	#endregion
	
	#region Verify Token
	
	public async Task<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		switch (tokenType)
		{
			case SupportedTokenTypes.Bearer:
				return await this.VerifyBearerTokenAsync(token, fireEvent, cancellationToken: cancellationToken);
			case SupportedTokenTypes.Basic:
				return await this.VerifyBasicTokenAsync(token, fireEvent, cancellationToken: cancellationToken);
			default:
				throw ErtisAuthException.UnsupportedTokenType();
		}
	}
	
	public async Task<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		var revokedToken = await this._revokedTokenService.GetByAccessTokenAsync(token, cancellationToken: cancellationToken);
		if (revokedToken != null)
		{
			throw ErtisAuthException.TokenWasRevoked();
		}
		
		if (!this._jwtService.TryDecodeToken(token, out var securityToken) || securityToken == null)
		{
			// Token couldn't be decoded!
			throw ErtisAuthException.InvalidToken();
		}
		
		var expireTime = securityToken.ValidTo;
		if (DateTime.UtcNow > expireTime)
		{
			// Token was expired!
			throw ErtisAuthException.TokenWasExpired();
		}
		
		// The signature is verified before any user lookup, so that forged tokens reveal nothing about accounts
		await this.VerifyTokenSignatureAsync(token, securityToken, cancellationToken: cancellationToken);
		
		// Reset password and activation tokens are signed with the same key, but they are not bearer tokens
		if (this.TryExtractClaimValue(securityToken, ActionTokens.TokenTypeClaim, out var actionTokenType) && !string.IsNullOrEmpty(actionTokenType))
		{
			throw ErtisAuthException.InvalidToken("Reset password and activation tokens can not be used as bearer tokens");
		}
		
		var user = await this.GetTokenOwnerAsync(securityToken, cancellationToken: cancellationToken);
		if (user == null)
		{
			// User not found!
			throw ErtisAuthException.UserNotFound(securityToken.Subject, "_id");
		}
		
		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}
		
		if (fireEvent)
		{
			// Only token metadata, never the token itself
			var tokenInfo = new { token_type = "bearer", is_refresh_token = this.IsRefreshToken(securityToken), expires_at = expireTime };
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenVerified, user, user.MembershipId, new { token = tokenInfo }, cancellationToken: cancellationToken);
		}
		
		return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.UtcNow, this.IsRefreshToken(securityToken))
		{
			Scopes = this.ExtractScopes(securityToken)
		};
	}
	
	/// <summary>
	/// Verifies the token signature with the secret key of the membership in its 'prn' claim.
	/// Unknown memberships are reported as an invalid token, so that membership ids can not be probed.
	/// </summary>
	private async Task<Membership> VerifyTokenSignatureAsync(string token, JsonWebToken securityToken, CancellationToken cancellationToken = default)
	{
		if (!this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) || string.IsNullOrEmpty(membershipId))
		{
			// MembershipId could not find in token claims!
			throw ErtisAuthException.InvalidToken();
		}
		
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var validation = await this._jwtService.ValidateTokenAsync(token, membership);
		if (!validation.IsValid)
		{
			// Token signature not verified!
			throw ErtisAuthException.InvalidToken("Token signature could not verified!");
		}
		
		return membership;
	}
	
	public async Task<BasicTokenValidationResult> VerifyBasicTokenAsync(string basicToken, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(basicToken))
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var parts = basicToken.Split(':');
		if (parts.Length != 2)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var applicationId = parts[0];
		var secret = parts[1];
		
		// Unknown applications, unknown memberships and wrong secrets are all reported as an invalid token,
		// so that application ids can not be probed
		var application = await this._applicationService.GetByIdAsync(applicationId, cancellationToken: cancellationToken);
		if (application == null)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		var membership = await this._membershipService.GetAsync(application.MembershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		// An application with its own secret only accepts that secret
		var isVerified = !string.IsNullOrEmpty(application.SecretHash)
			? ApplicationSecretHelper.VerifySecret(secret, application.SecretHash)
			: this._legacyApplicationSecretVerifier.Verify(application, membership, secret); // LEGACY-APP-SECRET: replace with false after migration
		
		if (!isVerified)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		if (fireEvent)
		{
			// Never the basic token: it carries the application secret (or, for legacy applications, the membership secret key)
			var tokenInfo = new { token_type = "basic", application_id = application.Id };
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenVerified, application, membership.Id, new { token = tokenInfo }, cancellationToken: cancellationToken);
		}
		
		return new BasicTokenValidationResult(true, basicToken, application);
	}
	
	#endregion
	
	#region Refresh Token
	
	public async Task<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		var revokedToken = await this._revokedTokenService.GetByAccessTokenAsync(refreshToken, cancellationToken: cancellationToken);
		if (revokedToken != null)
		{
			throw ErtisAuthException.RefreshTokenWasRevoked();
		}
		
		if (!this._jwtService.TryDecodeToken(refreshToken, out var securityToken) || securityToken == null)
		{
			// Token couldn't be decoded!
			throw ErtisAuthException.InvalidToken();
		}
		
		if (!this.IsRefreshToken(securityToken))
		{
			// This is not a refresh token!
			throw ErtisAuthException.TokenIsNotRefreshable();
		}
		
		if (DateTime.UtcNow > securityToken.ValidTo)
		{
			// Token was expired!
			throw ErtisAuthException.RefreshTokenWasExpired();
		}
		
		var membership = await this.VerifyTokenSignatureAsync(refreshToken, securityToken, cancellationToken: cancellationToken);
		
		var userId = securityToken.Subject;
		if (string.IsNullOrEmpty(userId))
		{
			// UserId could not find in token claims!
			throw ErtisAuthException.InvalidToken();
		}
		
		var user = await this._userService.GetUserAsync(membership.Id, userId, cancellationToken: cancellationToken);
		if (user == null)
		{
			// User not found!
			throw ErtisAuthException.UserNotFound(userId, "_id");
		}
		
		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}
		
		// A refreshed token keeps the scopes of the original token (never broader, RFC 6749 section 6)
		var scopes = this.ExtractScopes(securityToken);
		var originalActiveToken = await this._activeTokenService.GetByRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);
		var token = await this.GenerateBearerTokenAsync(user, membership, scopes, originalActiveToken?.ClientInfo?.IPAddress, originalActiveToken?.ClientInfo?.UserAgent, cancellationToken: cancellationToken);
		
		if (revokeBefore)
		{
			await this.RevokeTokenAsync(refreshToken, cancellationToken: cancellationToken);
		}
		
		if (fireEvent)
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenRefreshed, user, membership.Id, token, new { refreshToken }, cancellationToken: cancellationToken);	
		}
		
		return token;
	}
	
	#endregion
	
	#region Revoke Token
	
	public async Task<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		User? user;
		
		try
		{
			var validationResult = await this.VerifyBearerTokenAsync(token, false, cancellationToken: cancellationToken);
			user = validationResult.User;
			if (!validationResult.IsValidated)
			{
				throw ErtisAuthException.InvalidToken();
			}
		}
		catch (ErtisAuthException ex)
		{
			if (ex.ErrorCode == ErtisAuthException.TokenWasRevoked().ErrorCode)
			{
				this._logger.LogWarning("This token was revoked already");
			}
			
			return false;
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "TokenService.RevokeTokenAsync occured an error");
			return false;
		}
		
		if (user == null)
		{
			return false;
		}
		
		var activeTokens = await this._activeTokenService.GetActiveTokensByUser(user.Id, user.MembershipId, cancellationToken: cancellationToken);
		// The token may be either side of an access/refresh token pair; both are revoked together
		var filteredActiveTokens = (logoutFromAllDevices ? activeTokens : activeTokens.Where(x => x.AccessToken == token || x.RefreshToken == token)).ToArray();
		if (filteredActiveTokens.Length > 0)
		{
			await this.RevokeActiveTokensAsync(filteredActiveTokens, user, cancellationToken: cancellationToken);
		}
		
		return true;
	}
	
	public async Task RevokeAllAsync(string membershipId, string userId, string? exceptAccessToken = null, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		var activeTokens = (await this._activeTokenService.GetActiveTokensByUser(userId, membershipId, cancellationToken: cancellationToken))
			.Where(x => exceptAccessToken == null || x.AccessToken != exceptAccessToken)
			.ToArray();
		if (activeTokens.Any())
		{
			var user = await this._userService.GetUserAsync(membershipId, userId, cancellationToken: cancellationToken);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(userId, "_id");
			}
			
			await this.RevokeActiveTokensAsync(activeTokens, user, cancellationToken: cancellationToken);
		}
	}
	
	private async Task RevokeActiveTokensAsync(ActiveToken[] activeTokens, User user, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(user.MembershipId);
		}
		
		foreach (var activeToken in activeTokens)
		{
			await this._revokedTokenService.RevokeAsync(activeToken.AccessToken, user, false, activeToken.ExpireTime, cancellationToken: cancellationToken);
			
			if (!string.IsNullOrEmpty(activeToken.RefreshToken))
			{
				await this._revokedTokenService.RevokeAsync(activeToken.RefreshToken, user, true, activeToken.RefreshTokenExpireTime, cancellationToken: cancellationToken);
			}
			
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenRevoked, user, membership.Id, new { activeToken.AccessToken }, cancellationToken: cancellationToken);
		}
		
		await this._activeTokenService.BulkDeleteAsync(activeTokens, cancellationToken: cancellationToken);
	}
	
	#endregion
}