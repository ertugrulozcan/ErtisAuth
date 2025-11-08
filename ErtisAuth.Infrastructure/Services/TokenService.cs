using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Infrastructure.Services
{
	public class TokenService : ITokenService
	{
		#region Constants

		private const string REFRESH_TOKEN_CLAIM = "refresh_token";

		#endregion
		
		#region Services

		private readonly IMembershipService membershipService;
		private readonly IUserService userService;
		private readonly IApplicationService applicationService;
		private readonly IRoleService roleService;
		private readonly IJwtService jwtService;
		private readonly ICryptographyService cryptographyService;
		private readonly IEventService eventService;
		private readonly IActiveTokenService activeTokenService;
		private readonly IRevokedTokenService revokedTokenService;
		
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
		/// <param name="cryptographyService"></param>
		/// <param name="eventService"></param>
		/// <param name="activeTokenService"></param>
		/// <param name="revokedTokenService"></param>
		/// <param name="scheduledJobService"></param>
		public TokenService(
			IMembershipService membershipService, 
			IUserService userService, 
			IApplicationService applicationService,
			IRoleService roleService,
			IJwtService jwtService,
			ICryptographyService cryptographyService,
			IEventService eventService,
			IActiveTokenService activeTokenService,
			IRevokedTokenService revokedTokenService,
			IScheduledJobService scheduledJobService)
		{
			this.membershipService = membershipService;
			this.userService = userService;
			this.applicationService = applicationService;
			this.roleService = roleService;
			this.jwtService = jwtService;
			this.cryptographyService = cryptographyService;
			this.eventService = eventService;
			this.activeTokenService = activeTokenService;
			this.revokedTokenService = revokedTokenService;

			scheduledJobService.ScheduleTokenCleanerJobsAsync().ConfigureAwait(false);
		}

		#endregion

		#region WhoAmI

		public async ValueTask<User> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default)
		{
			await this.VerifyBearerTokenAsync(bearerToken.AccessToken, false, cancellationToken: cancellationToken);
			return await this.GetTokenOwnerUserAsync(bearerToken.AccessToken, cancellationToken: cancellationToken);
		}

		public async Task<User> GetTokenOwnerUserAsync(string bearerToken, CancellationToken cancellationToken = default)
		{
			if (this.jwtService.TryDecodeToken(bearerToken, out var securityToken))
			{
				return await this.GetTokenOwnerAsync(securityToken, cancellationToken: cancellationToken);
			}
			else
			{
				return null;
			}
		}

		public async ValueTask<Application> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default)
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
			var application = await this.applicationService.GetByIdAsync(applicationId, cancellationToken: cancellationToken);
			if (application == null)
			{
				throw ErtisAuthException.ApplicationNotFound(applicationId);
			}

			return application;
		}
		
		private async Task<User> GetTokenOwnerAsync(JwtSecurityToken securityToken, CancellationToken cancellationToken = default)
		{
			if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
			{
				var userId = securityToken.Subject;
				if (!string.IsNullOrEmpty(userId))
				{
					return await this.userService.GetUserAsync(membershipId, userId, cancellationToken: cancellationToken);
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

		public async ValueTask<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, string ipAddress = null, string userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default)
		{
			// Check membership
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			if (!membership.IsValid(out IEnumerable<string> errors))
			{
				throw ErtisAuthException.MalformedMembership(membershipId, errors);
			}
			
			// Check user
			var user = await this.userService.GetUserWithPasswordAsync(membership.Id, username, username, cancellationToken: cancellationToken);
			if (user == null)
			{
				throw ErtisAuthException.InvalidCredentials();
			}
			
			if (!user.IsActive)
			{
				throw ErtisAuthException.UserInactive(user.Id);
			}
			
			// Check password
			var passwordHash = this.cryptographyService.CalculatePasswordHash(membership, password);
			if (string.IsNullOrEmpty(passwordHash?.Trim()) || string.IsNullOrEmpty(user.PasswordHash?.Trim()) || passwordHash != user.PasswordHash)
			{
				throw ErtisAuthException.InvalidCredentials();
			}
			else
			{
				return await this.GenerateBearerTokenAsync(user, membership, null, ipAddress, userAgent, cancellationToken: cancellationToken);
			}
		}

		public async ValueTask<ScopedBearerToken> GenerateTokenAsync(
			string token, 
			string[] scopes, 
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
				
				var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
				if (membership == null)
				{
					throw ErtisAuthException.MembershipNotFound(membershipId);
				}
				
				var role = await this.roleService.GetBySlugAsync(verifyResult.User.Role, membershipId, cancellationToken: cancellationToken);
				if (role != null)
				{
					foreach (var scope in scopes)
					{
						if (Rbac.TryParse(scope, out var rbac))
						{
							if (!role.HasPermission(rbac))
							{
								throw ErtisAuthException.UserHasNoPermissionForThisScope(scope);
							}
							
							var verifiedForUser = verifyResult.User.HasPermission(rbac);
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

		public async ValueTask<BearerToken> GenerateTokenAsync(User user, string membershipId, string ipAddress = null, string userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default)
		{
			// Check membership
			var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			if (!membership.IsValid(out IEnumerable<string> errors))
			{
				throw ErtisAuthException.MalformedMembership(membershipId, errors);
			}
			
			// Check user
			var currentUser = await this.userService.GetAsync(membership.Id, user.Id, cancellationToken: cancellationToken);
			if (currentUser == null)
			{
				throw ErtisAuthException.UserNotFound(user.Id, "id");
			}
			
			return await this.GenerateBearerTokenAsync(user, membership, null, ipAddress, userAgent, cancellationToken: cancellationToken);
		}
		
		private async Task<BearerToken> GenerateBearerTokenAsync(User user, Membership membership, string[] scopes, string ipAddress = null, string userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default)
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
			
			var hashAlgorithm = membership.GetHashAlgorithm();
			var encoding = membership.GetEncoding();
			var accessToken = this.jwtService.GenerateToken(tokenClaims, hashAlgorithm, encoding);
			var refreshExpiresIn = TimeSpan.FromSeconds(membership.RefreshTokenExpiresIn);
			var refreshToken = this.jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), hashAlgorithm, encoding, refreshExpiresIn);
			var bearerToken = new BearerToken(accessToken, tokenClaims.ExpiresIn, refreshToken, refreshExpiresIn);
			
			// Save to active tokens collection
			await this.activeTokenService.CreateAsync(bearerToken, user, membership.Id, ipAddress, userAgent, cancellationToken: cancellationToken);

			if (fireEvent)
			{
				await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenGenerated, user, new { user, token = bearerToken }) { MembershipId = membership.Id }, cancellationToken: cancellationToken);	
			}
			
			return bearerToken;
		}

		private bool IsRefreshToken(JwtSecurityToken securityToken)
		{
			var refreshTokenClaim = securityToken.Claims.FirstOrDefault(x => x.Type == REFRESH_TOKEN_CLAIM);
			return refreshTokenClaim != null && 
			       bool.TryParse(refreshTokenClaim.Value, out bool isRefreshableToken) &&
			       isRefreshableToken;
		}
		
		private bool TryExtractClaimValue(JwtSecurityToken securityToken, string key, out string value)
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

		public async ValueTask<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true, CancellationToken cancellationToken = default)
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
		
		public async ValueTask<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true, CancellationToken cancellationToken = default)
		{
			var revokedToken = await this.revokedTokenService.GetByAccessTokenAsync(token, cancellationToken: cancellationToken);
			if (revokedToken != null)
			{
				throw ErtisAuthException.TokenWasRevoked();
			}
			
			if (this.jwtService.TryDecodeToken(token, out var securityToken))
			{
				var expireTime = securityToken.ValidTo.ToLocalTime();
				if (DateTime.Now <= expireTime)
				{
					var user = await this.GetTokenOwnerAsync(securityToken, cancellationToken: cancellationToken);
					if (user != null)
					{
						if (!user.IsActive)
						{
							throw ErtisAuthException.UserInactive(user.Id);
						}
						
						if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
						{
							var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
							if (membership != null)
							{
								var encoding = membership.GetEncoding();
								var secretSecurityKey = new SymmetricSecurityKey(encoding.GetBytes(membership.SecretKey));
								var tokenClaims = new TokenClaims(null, user, membership);
								if (!this.jwtService.ValidateToken(token, tokenClaims, secretSecurityKey, out _))
								{
									// Token signature not verified!
									throw ErtisAuthException.InvalidToken("Token signature could not verified!");
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
						
						if (fireEvent)
						{
							await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenVerified, user, new { token }) { MembershipId = membershipId }, cancellationToken: cancellationToken);	
						}

						if (this.TryExtractClaimValue(securityToken, "scope", out var scopeClaim) && !string.IsNullOrEmpty(scopeClaim))
						{
							var scopes = scopeClaim.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
							if (scopes.Length > 0)
							{
								return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.Now, this.IsRefreshToken(securityToken))
								{
									Scopes = scopes
								};
							}
							else
							{
								return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.Now, this.IsRefreshToken(securityToken));
							}
						}
						else
						{
							return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.Now, this.IsRefreshToken(securityToken));
						}
					}
					else
					{
						// User not found!
						var userId = securityToken.Subject;
						throw ErtisAuthException.UserNotFound(userId, "_id");
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
				// Token could not decoded!
				throw ErtisAuthException.InvalidToken();
			}
		}
		
		public async ValueTask<BasicTokenValidationResult> VerifyBasicTokenAsync(string basicToken, bool fireEvent = true, CancellationToken cancellationToken = default)
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

			var application = await this.applicationService.GetByIdAsync(applicationId, cancellationToken: cancellationToken);
			if (application == null)
			{
				throw ErtisAuthException.ApplicationNotFound(applicationId);
			}

			var membership = await this.membershipService.GetAsync(application.MembershipId, cancellationToken: cancellationToken);
			if (membership == null)
			{
				if (this.applicationService.IsSystemReservedApplication(application)) 
				{
					membership = await this.membershipService.GetBySecretKeyAsync(secret, cancellationToken: cancellationToken);
					var onTheFlyApplication = new Application
					{
						Id = application.Id,
						Name = application.Name,
						Slug = application.Slug,
						Role = application.Role,
						Permissions = application.Permissions,
						Forbidden = application.Forbidden,
						Sys = application.Sys,
						MembershipId = application.MembershipId
					};

					application = onTheFlyApplication;
				}

				if (membership == null)
				{
					throw ErtisAuthException.MembershipNotFound(application.MembershipId);
				}
			}

			if (membership.SecretKey != secret)
			{
				throw ErtisAuthException.ApplicationSecretMismatch();
			}

			if (fireEvent)
			{
				await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenVerified, application, new { basicToken }) { MembershipId = membership.Id }, cancellationToken: cancellationToken);	
			}
			
			return new BasicTokenValidationResult(true, basicToken, application);
		}

		#endregion

		#region Refresh Token

		public async ValueTask<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true, CancellationToken cancellationToken = default)
		{
			var revokedToken = await this.revokedTokenService.GetByAccessTokenAsync(refreshToken, cancellationToken: cancellationToken);
			if (revokedToken != null)
			{
				throw ErtisAuthException.RefreshTokenWasRevoked();
			}

			if (this.jwtService.TryDecodeToken(refreshToken, out var securityToken))
			{
				if (this.IsRefreshToken(securityToken))
				{
					var expireTime = securityToken.ValidTo.ToLocalTime();
					if (DateTime.Now <= expireTime)
					{
						if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
						{
							var membership = await this.membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
							if (membership != null)
							{
								var userId = securityToken.Subject;
								if (!string.IsNullOrEmpty(userId))
								{
									var dynamicObject = await this.userService.GetAsync(membershipId, userId, cancellationToken: cancellationToken);
									var user = dynamicObject?.Deserialize<User>();
									if (user != null)
									{
										if (!user.IsActive)
										{
											throw ErtisAuthException.UserInactive(user.Id);
										}
										
										var originalActiveToken = await this.activeTokenService.GetByRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);
										var token = await this.GenerateBearerTokenAsync(user, membership, null, originalActiveToken?.ClientInfo?.IPAddress, originalActiveToken?.ClientInfo?.UserAgent, cancellationToken: cancellationToken);

										if (revokeBefore)
										{
											await this.RevokeTokenAsync(refreshToken, cancellationToken: cancellationToken);
										}

										if (fireEvent)
										{
											await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRefreshed, user, token, new { refreshToken }) { MembershipId = membershipId }, cancellationToken: cancellationToken);	
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
				// Token could not decoded!
				throw ErtisAuthException.InvalidToken();
			}
		}

		#endregion

		#region Revoke Token

		public async ValueTask<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true, CancellationToken cancellationToken = default)
		{
			User user;
			
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
					Console.WriteLine("This token was revoked already");
				}

				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return false;
			}

			var activeTokens = await this.activeTokenService.GetActiveTokensByUser(user.Id, user.MembershipId, cancellationToken: cancellationToken);
			var filteredActiveTokens = (logoutFromAllDevices ? activeTokens : new[] { activeTokens.FirstOrDefault(x => x.AccessToken == token) }).Where(x => x != null).ToArray();
			if (filteredActiveTokens.Any())
			{
				var membership = await this.membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
				if (membership == null)
				{
					throw ErtisAuthException.MembershipNotFound(user.MembershipId);
				}
				
				foreach (var activeToken in filteredActiveTokens)
				{
					var isRefreshToken = false;
					if (this.jwtService.TryDecodeToken(activeToken.AccessToken, out var securityToken))
					{
						isRefreshToken = this.IsRefreshToken(securityToken);
					}
					
					await this.revokedTokenService.RevokeAsync(activeToken, user, isRefreshToken, cancellationToken: cancellationToken);
					
					if (!isRefreshToken)
					{
						var refreshToken = this.StimulateRefreshToken(activeToken.AccessToken, user, membership);
						if (!string.IsNullOrEmpty(refreshToken))
						{
							await this.RevokeRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);	
						}				
					}

					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRevoked, user, new { activeToken.AccessToken }) { MembershipId = membership.Id }, cancellationToken: cancellationToken);
				}

				await this.activeTokenService.BulkDeleteAsync(filteredActiveTokens, cancellationToken: cancellationToken);
			}

			return true;
		}

		private async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
		{
			await this.RevokeTokenAsync(refreshToken, false, false, cancellationToken: cancellationToken);
		}
		
		private string StimulateRefreshToken(string accessToken, User user, Membership membership)
		{
			if (this.jwtService.TryDecodeToken(accessToken, out var securityToken))
			{
				if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Jti, out var tokenId))
				{
					var tokenClaims = new TokenClaims(tokenId, user, membership);
					var hashAlgorithm = membership.GetHashAlgorithm();
					var encoding = membership.GetEncoding();
					var refreshToken = this.jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), securityToken.IssuedAt, hashAlgorithm, encoding);
					if (!string.IsNullOrEmpty(refreshToken))
					{
						return refreshToken;
					}
				}	
			}

			return null;
		}

		public async ValueTask RevokeAllAsync(string membershipId, string userId, bool fireEvent = true, CancellationToken cancellationToken = default)
		{
			var activeTokens = (await this.activeTokenService.GetActiveTokensByUser(userId, membershipId, cancellationToken: cancellationToken)).ToArray();
			if (activeTokens.Any())
			{
				var user = (await this.userService.GetAsync(membershipId, userId, cancellationToken: cancellationToken))?.Deserialize<User>();
				if (user == null)
				{
					throw ErtisAuthException.UserNotFound(userId, "_id");
				}
				
				var membership = await this.membershipService.GetAsync(user.MembershipId, cancellationToken: cancellationToken);
				if (membership == null)
				{
					throw ErtisAuthException.MembershipNotFound(user.MembershipId);
				}
				
				foreach (var activeToken in activeTokens)
				{
					var isRefreshToken = false;
					if (this.jwtService.TryDecodeToken(activeToken.AccessToken, out var securityToken))
					{
						isRefreshToken = this.IsRefreshToken(securityToken);
					}
					
					await this.revokedTokenService.RevokeAsync(activeToken, user, isRefreshToken, cancellationToken: cancellationToken);

					if (!isRefreshToken)
					{
						var refreshToken = this.StimulateRefreshToken(activeToken.AccessToken, user, membership);
						if (!string.IsNullOrEmpty(refreshToken))
						{
							await this.RevokeRefreshTokenAsync(refreshToken, cancellationToken: cancellationToken);	
						}				
					}

					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRevoked, user, new { activeToken.AccessToken }) { MembershipId = membership.Id }, cancellationToken: cancellationToken);
				}

				await this.activeTokenService.BulkDeleteAsync(activeTokens, cancellationToken: cancellationToken);
			}
		}

		#endregion

		#region Cleaning

		public async ValueTask ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default)
		{
			await this.activeTokenService.ClearExpiredActiveTokens(membershipId, cancellationToken: cancellationToken);
		}

		public async ValueTask ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default)
		{
			await this.revokedTokenService.ClearRevokedTokens(membershipId, cancellationToken: cancellationToken);
		}

		#endregion
	}
}