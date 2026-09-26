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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
		this._logger = logger;
	}
	
	#endregion
	
	#region WhoAmI
	
	public async Task<User?> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default)
	{
		await this.VerifyBearerTokenAsync(bearerToken.AccessToken, false, cancellationToken: cancellationToken);
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
			throw ErtisAuthException.InvalidCredentials();
		}
		
		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}
		
		// Check password
		var passwordHash = this._userService.CalculatePasswordHash(membership, password);
		if (string.IsNullOrEmpty(passwordHash.Trim()) || string.IsNullOrEmpty(user.PasswordHash?.Trim()) || passwordHash != user.PasswordHash)
		{
			throw ErtisAuthException.InvalidCredentials();
		}
		else
		{
			return await this.GenerateBearerTokenAsync(user, membership, null, ipAddress, userAgent, cancellationToken: cancellationToken);
		}
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
						if (!role.HasPermission(rbac!))
						{
							throw ErtisAuthException.UserHasNoPermissionForThisScope(scope);
						}
						
						var verifiedForUser = verifyResult.User.HasPermission(rbac!);
						if (verifiedForUser != null && verifiedForUser.Value)
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
		var accessToken = this._jwtService.GenerateToken(tokenClaims);
		var refreshExpiresIn = TimeSpan.FromSeconds(membership.RefreshTokenExpiresIn);
		var refreshToken = this._jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), expiresIn: refreshExpiresIn, encoding: encoding);
		var bearerToken = new BearerToken(accessToken, tokenClaims.ExpiresIn, refreshToken, refreshExpiresIn);
		
		// Save to active tokens collection
		await this._activeTokenService.CreateAsync(bearerToken, user, membership.Id, ipAddress, userAgent, cancellationToken: cancellationToken);
		
		if (fireEvent)
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenGenerated, user, membership.Id, new { user, token = bearerToken }, cancellationToken: cancellationToken);
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
		
		if (this._jwtService.TryDecodeToken(token, out var securityToken) && securityToken != null)
		{
			var expireTime = securityToken.ValidTo;
			if (DateTime.UtcNow <= expireTime)
			{
				var user = await this.GetTokenOwnerAsync(securityToken, cancellationToken: cancellationToken);
				if (user != null)
				{
					if (!user.IsActive)
					{
						throw ErtisAuthException.UserInactive(user.Id);
					}
					
					var membership = await this._membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
					if (membership != null)
					{
						var encoding = membership.GetEncoding();
						var secretSecurityKey = new SymmetricSecurityKey(encoding.GetBytes(membership.SecretKey));
						var tokenClaims = new TokenClaims(null, user, membership);
						var validation = await this._jwtService.ValidateTokenAsync(token, tokenClaims, secretSecurityKey);
						if (!validation.IsValid)
						{
							// Token signature not verified!
							throw ErtisAuthException.InvalidToken("Token signature could not verified!");
						}
					}
					else
					{
						// Membership not found!
						throw ErtisAuthException.MembershipNotFound(user.MembershipId);
					}
					
					if (fireEvent)
					{
						await this._eventService.FireEventAsync(ErtisAuthEventType.TokenVerified, user, user.MembershipId, new { token }, cancellationToken: cancellationToken);	
					}
					
					if (this.TryExtractClaimValue(securityToken, "scope", out var scopeClaim) && !string.IsNullOrEmpty(scopeClaim))
					{
						var scopes = scopeClaim.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
						if (scopes.Length > 0)
						{
							return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.UtcNow, this.IsRefreshToken(securityToken))
							{
								Scopes = scopes
							};
						}
						else
						{
							return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.UtcNow, this.IsRefreshToken(securityToken));
						}
					}
					else
					{
						return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.UtcNow, this.IsRefreshToken(securityToken));
					}
				}
				else
				{
					// User not found!
					throw ErtisAuthException.UserNotFound(securityToken.Subject, "_id");
				}
			}
			else
			{
				// Token was expired!
				throw ErtisAuthException.TokenWasExpired();
			}
		}
		else
		{
			// Token couldn't be decoded!
			throw ErtisAuthException.InvalidToken();
		}
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
		
		var application = await this._applicationService.GetByIdAsync(applicationId, cancellationToken: cancellationToken);
		if (application == null)
		{
			throw ErtisAuthException.ApplicationNotFound(applicationId);
		}
		
		var membership = await this._membershipService.GetAsync(application.MembershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(application.MembershipId);
		}
		
		if (membership.SecretKey != secret)
		{
			throw ErtisAuthException.ApplicationSecretMismatch();
		}
		
		if (fireEvent)
		{
			await this._eventService.FireEventAsync(ErtisAuthEventType.TokenVerified, application, membership.Id, new { basicToken }, cancellationToken: cancellationToken);	
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
		
		if (this._jwtService.TryDecodeToken(refreshToken, out var securityToken) && securityToken != null)
		{
			if (this.IsRefreshToken(securityToken))
			{
				var expireTime = securityToken.ValidTo;
				if (DateTime.UtcNow <= expireTime)
				{
					if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
					{
						var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
						if (membership != null)
						{
							var userId = securityToken.Subject;
							if (!string.IsNullOrEmpty(userId))
							{
								var user = await this._userService.GetUserAsync(membershipId, userId, cancellationToken: cancellationToken);
								if (user != null)
								{
									if (!user.IsActive)
									{
										throw ErtisAuthException.UserInactive(user.Id);
									}
									
									var originalActiveToken = await this._activeTokenService.GetByRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);
									var token = await this.GenerateBearerTokenAsync(user, membership, null, originalActiveToken?.ClientInfo?.IPAddress, originalActiveToken?.ClientInfo?.UserAgent, cancellationToken: cancellationToken);
									
									if (revokeBefore)
									{
										await this.RevokeTokenAsync(refreshToken, cancellationToken: cancellationToken);
									}
									
									if (fireEvent)
									{
										await this._eventService.FireEventAsync(ErtisAuthEventType.TokenRefreshed, user, membershipId, token, new { refreshToken }, cancellationToken: cancellationToken);	
									}
									
									return token;
								}
								else
								{
									// User not found!
									throw ErtisAuthException.UserNotFound(userId, "_id");
								}
							}
							else
							{
								// UserId could not find in token claims!
								throw ErtisAuthException.InvalidToken();
							}
						}
						else
						{
							// Membership not found!
							throw ErtisAuthException.MembershipNotFound(membershipId);
						}	
					}
					else
					{
						// MembershipId could not find in token claims!
						throw ErtisAuthException.InvalidToken();
					}
				}
				else
				{
					// Token was expired!
					throw ErtisAuthException.RefreshTokenWasExpired();
				}
			}
			else
			{
				// This is not a refresh token!
				throw ErtisAuthException.TokenIsNotRefreshable();
			}
		}
		else
		{
			// Token couldn't be decoded!
			throw ErtisAuthException.InvalidToken();
		}
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
		var filteredActiveTokens = (logoutFromAllDevices ? activeTokens : activeTokens.Where(x => x.AccessToken == token)).ToArray();
		if (filteredActiveTokens.Length > 0)
		{
			var membership = await this._membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(user.MembershipId);
			}
			
			foreach (var activeToken in filteredActiveTokens)
			{
				var isRefreshToken = false;
				if (this._jwtService.TryDecodeToken(activeToken.AccessToken, out var securityToken) && securityToken != null)
				{
					isRefreshToken = this.IsRefreshToken(securityToken);
				}
				
				await this._revokedTokenService.RevokeAsync(activeToken, user, isRefreshToken, cancellationToken: cancellationToken);
				
				if (!isRefreshToken)
				{
					var refreshToken = this.StimulateRefreshToken(activeToken.AccessToken, user, membership);
					if (!string.IsNullOrEmpty(refreshToken))
					{
						await this.RevokeRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);	
					}				
				}
				
				await this._eventService.FireEventAsync(ErtisAuthEventType.TokenRevoked, user, membership.Id, new { activeToken.AccessToken }, cancellationToken: cancellationToken);
			}
			
			await this._activeTokenService.BulkDeleteAsync(filteredActiveTokens, cancellationToken: cancellationToken);
		}
		
		return true;
	}
	
	private async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
	{
		await this.RevokeTokenAsync(refreshToken, false, false, cancellationToken: cancellationToken);
	}
	
	private string? StimulateRefreshToken(string accessToken, User user, Membership membership)
	{
		if (this._jwtService.TryDecodeToken(accessToken, out var securityToken) && securityToken != null)
		{
			if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Jti, out var tokenId))
			{
				var tokenClaims = new TokenClaims(tokenId, user, membership);
				var encoding = membership.GetEncoding();
				var refreshToken = this._jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), generationTime: securityToken.IssuedAt, encoding: encoding);
				if (!string.IsNullOrEmpty(refreshToken))
				{
					return refreshToken;
				}
			}	
		}
		
		return null;
	}
	
	public async Task RevokeAllAsync(string membershipId, string userId, bool fireEvent = true, CancellationToken cancellationToken = default)
	{
		var activeTokens = (await this._activeTokenService.GetActiveTokensByUser(userId, membershipId, cancellationToken: cancellationToken)).ToArray();
		if (activeTokens.Any())
		{
			var user = await this._userService.GetUserAsync(membershipId, userId, cancellationToken: cancellationToken);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(userId, "_id");
			}
			
			var membership = await this._membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(user.MembershipId);
			}
			
			foreach (var activeToken in activeTokens)
			{
				var isRefreshToken = false;
				if (this._jwtService.TryDecodeToken(activeToken.AccessToken, out var securityToken) && securityToken != null)
				{
					isRefreshToken = this.IsRefreshToken(securityToken);
				}
				
				await this._revokedTokenService.RevokeAsync(activeToken, user, isRefreshToken, cancellationToken: cancellationToken);
				
				if (!isRefreshToken)
				{
					var refreshToken = this.StimulateRefreshToken(activeToken.AccessToken, user, membership);
					if (!string.IsNullOrEmpty(refreshToken))
					{
						await this.RevokeRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);	
					}				
				}
				
				await this._eventService.FireEventAsync(ErtisAuthEventType.TokenRevoked, user, membership.Id, new { activeToken.AccessToken }, cancellationToken: cancellationToken);
			}
			
			await this._activeTokenService.BulkDeleteAsync(activeTokens, cancellationToken: cancellationToken);
		}
	}
	
	#endregion
	
	#region Cleaning
	
	public async Task ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default)
	{
		await this._activeTokenService.ClearExpiredActiveTokens(membershipId, cancellationToken: cancellationToken);
	}
	
	public async Task ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default)
	{
		await this._revokedTokenService.ClearRevokedTokens(membershipId, cancellationToken: cancellationToken);
	}
	
	#endregion
}