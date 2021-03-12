using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;
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
		private readonly IJwtService jwtService;
		private readonly ICryptographyService cryptographyService;
		private readonly IEventService eventService;
		private readonly IRevokedTokensRepository revokedTokensRepository;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		/// <param name="userService"></param>
		/// <param name="applicationService"></param>
		/// <param name="jwtService"></param>
		/// <param name="cryptographyService"></param>
		/// <param name="eventService"></param>
		/// <param name="revokedTokensRepository"></param>
		public TokenService(
			IMembershipService membershipService, 
			IUserService userService, 
			IApplicationService applicationService,
			IJwtService jwtService,
			ICryptographyService cryptographyService,
			IEventService eventService,
			IRevokedTokensRepository revokedTokensRepository)
		{
			this.membershipService = membershipService;
			this.userService = userService;
			this.applicationService = applicationService;
			this.jwtService = jwtService;
			this.cryptographyService = cryptographyService;
			this.eventService = eventService;
			this.revokedTokensRepository = revokedTokensRepository;
		}

		#endregion

		#region WhoAmI

		public async Task<User> WhoAmIAsync(string token, SupportedTokenTypes tokenType)
		{
			switch (tokenType)
			{
				case SupportedTokenTypes.Bearer:
					await this.VerifyBearerTokenAsync(token, false);
					break;
				case SupportedTokenTypes.Basic:
					await this.VerifyBasicTokenAsync(token, false);
					break;
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
			
			return await this.GetTokenOwnerAsync(token);
		}

		private async Task<User> GetTokenOwnerAsync(string token)
		{
			if (this.jwtService.TryDecodeToken(token, out var securityToken))
			{
				return await this.GetTokenOwnerAsync(securityToken);
			}
			else
			{
				return null;
			}
		}
		
		private async Task<User> GetTokenOwnerAsync(JwtSecurityToken securityToken)
		{
			if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
			{
				var userId = securityToken.Subject;
				if (!string.IsNullOrEmpty(userId))
				{
					return await this.userService.GetAsync(membershipId, userId);
				}
				else
				{
					// UserId could not found in token claims!
					throw ErtisAuthException.InvalidToken();
				}
			}
			else
			{
				// MembershipId could not found in token claims!
				throw ErtisAuthException.InvalidToken();
			}
		}
		
		#endregion
		
		#region Generate Token

		public async Task<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, bool fireEvent = true)
		{
			// Check membership
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(membershipId);
			}

			if (!membership.IsValid(out IEnumerable<string> errors))
			{
				throw ErtisAuthException.MalformedMembership(membershipId, errors);
			}

			// Check user
			var user = await this.userService.GetUserWithPasswordAsync(username, username, membership.Id);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(username, "username or email");
			}
			
			// Check password
			var passwordHash = this.cryptographyService.CalculatePasswordHash(membership, password);
			if (passwordHash != user.PasswordHash)
			{
				throw ErtisAuthException.UsernameOrPasswordIsWrong(username, password);
			}
			else
			{
				var token = this.GenerateBearerToken(user, membership);

				if (fireEvent)
				{
					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenGenerated, user, token));	
				}
				
				return token;
			}
		}
		
		private BearerToken GenerateBearerToken(User user, Membership membership)
		{
			string tokenId = Guid.NewGuid().ToString();
			var tokenClaims = new TokenClaims(tokenId, user, membership);
			var hashAlgorithm = membership.GetHashAlgorithm();
			var encoding = membership.GetEncoding();
			var accessToken = this.jwtService.GenerateToken(tokenClaims, hashAlgorithm, encoding);
			var refreshExpiresIn = TimeSpan.FromSeconds(membership.RefreshTokenExpiresIn);
			var refreshToken = this.jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), hashAlgorithm, encoding);

			return new BearerToken(accessToken, tokenClaims.ExpiresIn, refreshToken, refreshExpiresIn);
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

		public async Task<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true)
		{
			switch (tokenType)
			{
				case SupportedTokenTypes.Bearer:
					return await this.VerifyBearerTokenAsync(token, fireEvent);
				case SupportedTokenTypes.Basic:
					return await this.VerifyBasicTokenAsync(token, fireEvent);
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
		}
		
		public async Task<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true)
		{
			var revokedToken = await this.revokedTokensRepository.FindOneAsync(x => x.Token == token);
			if (revokedToken != null)
			{
				throw ErtisAuthException.TokenWasRevoked();
			}

			if (this.jwtService.TryDecodeToken(token, out var securityToken))
			{
				var expireTime = securityToken.ValidTo.ToLocalTime();
				if (DateTime.Now <= expireTime)
				{
					var user = await this.GetTokenOwnerAsync(securityToken);
					if (user != null)
					{
						if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
						{
							var membership = await this.membershipService.GetAsync(membershipId);
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
							// MembershipId could not found in token claims!
							throw ErtisAuthException.InvalidToken();
						}
						
						if (fireEvent)
						{
							await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenVerified, user, new { token }));	
						}
				
						return new BearerTokenValidationResult(true, token, user, expireTime - DateTime.Now, this.IsRefreshToken(securityToken));
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
		
		public async Task<BasicTokenValidationResult> VerifyBasicTokenAsync(string basicToken, bool fireEvent = true)
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
			var application = await this.applicationService.GetByIdAsync(applicationId);
			if (application == null)
			{
				throw ErtisAuthException.ApplicationNotFound(applicationId);
			}

			var membership = await this.membershipService.GetAsync(application.MembershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(application.MembershipId);
			}

			var secret = parts[1];
			if (membership.SecretKey != secret)
			{
				throw ErtisAuthException.ApplicationSecretMismatch();
			}

			if (fireEvent)
			{
				await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenVerified, application, new { basicToken }));	
			}
			
			return new BasicTokenValidationResult(true, basicToken, application);
		}

		#endregion

		#region Refresh Token

		public async Task<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true)
		{
			var revokedToken = await this.revokedTokensRepository.FindOneAsync(x => x.Token == refreshToken);
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
							var membership = await this.membershipService.GetAsync(membershipId);
							if (membership != null)
							{
								var userId = securityToken.Subject;
								if (!string.IsNullOrEmpty(userId))
								{
									var user = await this.userService.GetAsync(membershipId, userId);
									if (user != null)
									{
										var token = this.GenerateBearerToken(user, membership);

										if (revokeBefore)
										{
											await this.RevokeTokenAsync(refreshToken);
										}

										if (fireEvent)
										{
											await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRefreshed, user, token, new { refreshToken }));	
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
									// UserId could not found in token claims!
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
							// MembershipId could not found in token claims!
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

		public async Task<bool> RevokeTokenAsync(string token, bool fireEvent = true)
		{
			var validationResult = await this.VerifyBearerTokenAsync(token, false);
			if (!validationResult.IsValidated)
			{
				throw ErtisAuthException.InvalidToken();
			}

			await this.revokedTokensRepository.InsertAsync(new RevokedTokenDto
			{
				Token = token,
				RevokedAt = DateTime.Now,
				UserId = validationResult.User.Id
			});
			
			var membership = await this.membershipService.GetAsync(validationResult.User.MembershipId);
			if (membership == null)
			{
				throw ErtisAuthException.MembershipNotFound(validationResult.User.MembershipId);
			}

			if (this.jwtService.TryDecodeToken(token, out var securityToken))
			{
				if (!this.IsRefreshToken(securityToken))
				{
					var refreshToken = this.StimulateRefreshToken(token, validationResult.User, membership);
					if (!string.IsNullOrEmpty(refreshToken))
					{
						await this.RevokeRefreshTokenAsync(refreshToken);	
					}				
				}	
			}

			await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRevoked, validationResult.User, new { token }));
			
			return true;
		}

		private async Task RevokeRefreshTokenAsync(string refreshToken)
		{
			await this.RevokeTokenAsync(refreshToken, false);
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

		#endregion
	}
}