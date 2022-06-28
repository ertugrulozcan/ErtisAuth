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
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.GeoLocation;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using ErtisAuth.Infrastructure.Configuration;
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
		private readonly IActiveTokensRepository activeTokensRepository;
		private readonly IRevokedTokensRepository revokedTokensRepository;
		private readonly IGeoLocationOptions geoLocationOptions;
		private readonly IGeoLocationService geoLocationService;
		
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
		/// <param name="activeTokensRepository"></param>
		/// <param name="revokedTokensRepository"></param>
		/// <param name="scheduledJobService"></param>
		/// <param name="geoLocationOptions"></param>
		/// <param name="geoLocationService"></param>
		public TokenService(
			IMembershipService membershipService, 
			IUserService userService, 
			IApplicationService applicationService,
			IJwtService jwtService,
			ICryptographyService cryptographyService,
			IEventService eventService,
			IActiveTokensRepository activeTokensRepository,
			IRevokedTokensRepository revokedTokensRepository,
			IScheduledJobService scheduledJobService,
			IGeoLocationOptions geoLocationOptions,
			IGeoLocationService geoLocationService)
		{
			this.membershipService = membershipService;
			this.userService = userService;
			this.applicationService = applicationService;
			this.jwtService = jwtService;
			this.cryptographyService = cryptographyService;
			this.eventService = eventService;
			this.activeTokensRepository = activeTokensRepository;
			this.revokedTokensRepository = revokedTokensRepository;
			this.geoLocationOptions = geoLocationOptions;
			this.geoLocationService = geoLocationService;

			scheduledJobService.ScheduleTokenCleanerJobsAsync().ConfigureAwait(false);
		}

		#endregion

		#region WhoAmI

		public async ValueTask<User> WhoAmIAsync(BearerToken bearerToken)
		{
			await this.VerifyBearerTokenAsync(bearerToken.AccessToken, false);
			return await this.GetTokenOwnerUserAsync(bearerToken.AccessToken);
		}

		private async Task<User> GetTokenOwnerUserAsync(string bearerToken)
		{
			if (this.jwtService.TryDecodeToken(bearerToken, out var securityToken))
			{
				return await this.GetTokenOwnerAsync(securityToken);
			}
			else
			{
				return null;
			}
		}

		public async ValueTask<Application> WhoAmIAsync(BasicToken basicToken)
		{
			await this.VerifyBasicTokenAsync(basicToken.AccessToken, false);
			return await this.GetTokenOwnerApplicationAsync(basicToken.AccessToken);
		}
		
		private async Task<Application> GetTokenOwnerApplicationAsync(string basicToken)
		{
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

			return application;
		}
		
		private async Task<User> GetTokenOwnerAsync(JwtSecurityToken securityToken)
		{
			if (this.TryExtractClaimValue(securityToken, JwtRegisteredClaimNames.Prn, out var membershipId) && !string.IsNullOrEmpty(membershipId))
			{
				var userId = securityToken.Subject;
				if (!string.IsNullOrEmpty(userId))
				{
					var dynamicObject = await this.userService.GetAsync(membershipId, userId);
					var user = dynamicObject.Deserialize<User>();
					return user;
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

		public async ValueTask<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, string ipAddress = null, string userAgent = null, bool fireEvent = true)
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
			var user = await this.userService.GetUserWithPasswordAsync(membership.Id, username, username);
			if (user == null)
			{
				throw ErtisAuthException.UserNotFound(username, "username or email");
			}
			
			// Check password
			var passwordHash = this.cryptographyService.CalculatePasswordHash(membership, password);
			if (passwordHash != user.PasswordHash)
			{
				throw ErtisAuthException.UsernameOrPasswordIsWrong();
			}
			else
			{
				var token = await this.GenerateBearerTokenAsync(user, membership, ipAddress, userAgent);
				
				if (fireEvent)
				{
					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenGenerated, user, token) { MembershipId = membershipId });	
				}
				
				return token;
			}
		}
		
		private async Task<BearerToken> GenerateBearerTokenAsync(User user, Membership membership, string ipAddress = null, string userAgent = null)
		{
			string tokenId = Guid.NewGuid().ToString();
			var tokenClaims = new TokenClaims(tokenId, user, membership);
			var hashAlgorithm = membership.GetHashAlgorithm();
			var encoding = membership.GetEncoding();
			var accessToken = this.jwtService.GenerateToken(tokenClaims, hashAlgorithm, encoding);
			var refreshExpiresIn = TimeSpan.FromSeconds(membership.RefreshTokenExpiresIn);
			var refreshToken = this.jwtService.GenerateToken(tokenClaims.AddClaim(REFRESH_TOKEN_CLAIM, true), hashAlgorithm, encoding);
			var bearerToken = new BearerToken(accessToken, tokenClaims.ExpiresIn, refreshToken, refreshExpiresIn);
			
			// Save to active tokens collection
			await this.StoreActiveTokenAsync(bearerToken, user, membership.Id, ipAddress, userAgent);
			
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

		private async Task StoreActiveTokenAsync(BearerToken token, User user, string membershipId, string ipAddress = null, string userAgent = null)
		{
			await this.activeTokensRepository.InsertAsync(new ActiveTokenDto
			{
				AccessToken = token.AccessToken,
				RefreshToken = token.RefreshToken,
				ExpiresIn = token.ExpiresInTimeStamp,
				RefreshTokenExpiresIn = token.RefreshTokenExpiresInTimeStamp,
				TokenType = token.TokenType.ToString(),
				CreatedAt = token.CreatedAt,
				UserId = user.Id,
				UserName = user.Username,
				EmailAddress = user.EmailAddress,
				FirstName = user.FirstName,
				LastName = user.LastName,
				MembershipId = membershipId,
				ClientInfo = ConvertToClientInfoDto(await this.GetClientInfo(ipAddress, userAgent))
			});
		}

		private static ClientInfoDto ConvertToClientInfoDto(ClientInfo clientInfo)
		{
			var geoLocation = clientInfo.GeoLocation;
			GeoLocationInfoDto geoLocationDto = null;
			if (geoLocation != null)
			{
				geoLocationDto = new GeoLocationInfoDto
				{
					City = geoLocation.City,
					Country = geoLocation.Country,
					CountryCode = geoLocation.CountryCode,
					PostalCode = geoLocation.PostalCode,
					Location = geoLocation.Location,
					Isp = geoLocation.Isp,
					IspDomain = geoLocation.IspDomain
				};
			}
			
			return new ClientInfoDto
			{
				IPAddress = clientInfo.IPAddress,
				UserAgent = clientInfo.UserAgent,
				GeoLocation = geoLocationDto
			};
		}
		
		private async Task<ClientInfo> GetClientInfo(string ipAddress, string userAgent)
		{
			var clientInfo = new ClientInfo
			{
				IPAddress = ipAddress,
				UserAgent = userAgent
			};

			try
			{
				if (this.geoLocationOptions.Enabled && !string.IsNullOrEmpty(ipAddress))
				{
					clientInfo.GeoLocation = await this.geoLocationService.LookupAsync(ipAddress);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			return clientInfo;
		}
		
		#endregion

		#region Verify Token

		public async ValueTask<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true)
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
		
		public async ValueTask<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true)
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
							await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenVerified, user, new { token }) { MembershipId = membershipId });	
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
		
		public async ValueTask<BasicTokenValidationResult> VerifyBasicTokenAsync(string basicToken, bool fireEvent = true)
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

			var application = await this.applicationService.GetByIdAsync(applicationId);
			if (application == null)
			{
				throw ErtisAuthException.ApplicationNotFound(applicationId);
			}

			var membership = await this.membershipService.GetAsync(application.MembershipId);
			if (membership == null)
			{
				if (this.applicationService.IsSystemReservedApplication(application)) 
				{
					membership = await this.membershipService.GetBySecretKeyAsync(secret);
					var onTheFlyApplication = new Application
					{
						Id = application.Id,
						Name = application.Name,
						Role = application.Role,
						Permissions = application.Permissions,
						Forbidden = application.Forbidden,
						Sys = application.Sys,
						MembershipId = membership.Id
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
				await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenVerified, application, new { basicToken }) { MembershipId = membership.Id });	
			}
			
			return new BasicTokenValidationResult(true, basicToken, application);
		}

		#endregion

		#region Refresh Token

		public async ValueTask<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true)
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
									var dynamicObject = await this.userService.GetAsync(membershipId, userId);
									var user = dynamicObject.Deserialize<User>();
									if (user != null)
									{
										var token = await this.GenerateBearerTokenAsync(user, membership);

										if (revokeBefore)
										{
											await this.RevokeTokenAsync(refreshToken);
										}

										if (fireEvent)
										{
											await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRefreshed, user, token, new { refreshToken }) { MembershipId = membershipId });	
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

		public async ValueTask<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true)
		{
			User user;
			
			try
			{
				var validationResult = await this.VerifyBearerTokenAsync(token, false);
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

			var activeTokenDtos = await this.GetActiveTokensByUser(user.Id, user.MembershipId);
			var filteredActiveTokenDtos = logoutFromAllDevices ? activeTokenDtos : new[] { activeTokenDtos.FirstOrDefault(x => x.AccessToken == token) };
			var activeTokens = filteredActiveTokenDtos.Where(x => x != null).ToArray();

			if (activeTokens.Any())
			{
				foreach (var activeTokenDto in activeTokens)
				{
					var isRefreshToken = false;
					if (this.jwtService.TryDecodeToken(activeTokenDto.AccessToken, out var securityToken))
					{
						isRefreshToken = this.IsRefreshToken(securityToken);
					}
				
					await this.revokedTokensRepository.InsertAsync(new RevokedTokenDto
					{
						Token = activeTokenDto.AccessToken,
						RevokedAt = DateTime.Now,
						UserId = user.Id,
						UserName = user.Username,
						EmailAddress = user.EmailAddress,
						FirstName = user.FirstName,
						LastName = user.LastName,
						MembershipId = user.MembershipId,
						TokenType = isRefreshToken ? "refresh_token" : "bearer_token"
					});
			
					var membership = await this.membershipService.GetAsync(user.MembershipId);
					if (membership == null)
					{
						throw ErtisAuthException.MembershipNotFound(user.MembershipId);
					}

					if (!isRefreshToken)
					{
						var refreshToken = this.StimulateRefreshToken(activeTokenDto.AccessToken, user, membership);
						if (!string.IsNullOrEmpty(refreshToken))
						{
							await this.RevokeRefreshTokenAsync(refreshToken);	
						}				
					}

					await this.eventService.FireEventAsync(this, new ErtisAuthEvent(ErtisAuthEventType.TokenRevoked, user, new { activeTokenDto.AccessToken }) { MembershipId = membership.Id });
				}

				await this.activeTokensRepository.BulkDeleteAsync(activeTokens);
			}

			return true;
		}

		private async Task RevokeRefreshTokenAsync(string refreshToken)
		{
			await this.RevokeTokenAsync(refreshToken, false, false);
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

		#region Cleaning

		private async Task<IEnumerable<ActiveTokenDto>> GetActiveTokensByUser(string userId, string membershipId)
		{
			var expiredActiveTokensResult = await this.activeTokensRepository.FindAsync(x => x.UserId == userId && x.MembershipId == membershipId);
			return expiredActiveTokensResult.Items;
		}
		
		public async ValueTask ClearExpiredActiveTokens(string membershipId)
		{
			try
			{
				var expiredActiveTokensResult = await this.activeTokensRepository.FindAsync(x => x.MembershipId == membershipId && x.ExpireTime < DateTime.Now);
				var expiredActiveTokens = expiredActiveTokensResult.Items.ToArray();
				if (expiredActiveTokens.Any())
				{
					var isDeleted = await this.activeTokensRepository.BulkDeleteAsync(expiredActiveTokens);
					if (isDeleted)
					{
						Console.WriteLine($"{expiredActiveTokens.Length} expired active token cleared");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public async ValueTask ClearRevokedTokens(string membershipId)
		{
			try
			{
				var revokedTokensResult = await this.revokedTokensRepository.FindAsync(x => x.MembershipId == membershipId && x.RevokedAt < DateTime.Now.AddHours(24));
				var revokedTokens = revokedTokensResult.Items.ToArray();
				if (revokedTokens.Any())
				{
					var isDeleted = await this.revokedTokensRepository.BulkDeleteAsync(revokedTokens);
					if (isDeleted)
					{
						Console.WriteLine($"{revokedTokens.Length} revoked token cleared");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		#endregion
	}
}