using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Ertis.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Helpers;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Extensions.Http.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IAuthenticationService = ErtisAuth.Sdk.Services.Interfaces.IAuthenticationService;

namespace ErtisAuth.Extensions.AspNetCore
{
	public class ErtisAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		#region Services

		private readonly IAuthenticationService authenticationService;
		private readonly IApplicationService applicationService;
		private readonly IRoleService roleService;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options"></param>
		/// <param name="authenticationService"></param>
		/// <param name="applicationService"></param>
		/// <param name="roleService"></param>
		/// <param name="logger"></param>
		/// <param name="encoder"></param>
		/// <param name="clock"></param>
		public ErtisAuthAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options, 
			IAuthenticationService authenticationService,
			IApplicationService applicationService,
			IRoleService roleService,
			ILoggerFactory logger, 
			UrlEncoder encoder, 
			ISystemClock clock) : 
			base(options, logger, encoder, clock)
		{
			this.authenticationService = authenticationService;
			this.applicationService = applicationService;
			this.roleService = roleService;
		}
		
		#endregion

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			try
			{
				var utilizer = await this.CheckAuthorizationAsync();
				
				var identity = new ClaimsIdentity(
					new []
					{
						new Claim(Utilizer.UtilizerIdClaimName, utilizer.Id),
						new Claim(Utilizer.UtilizerUsernameClaimName, utilizer.Username),
						new Claim(Utilizer.UtilizerTypeClaimName, utilizer.Type.ToString()),
						new Claim(Utilizer.UtilizerRoleClaimName, utilizer.Role),
						new Claim(Utilizer.UtilizerTokenClaimName, utilizer.Token),
						new Claim(Utilizer.UtilizerTokenTypeClaimName, utilizer.TokenType.ToString()),
					}, 
					null, 
					"Utilizer", 
					utilizer.Role);

				this.Context.User.AddIdentity(identity);

				var principal = new ClaimsPrincipal(identity);
				return AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name));
			}
			catch (ErtisAuthException ex)
			{
				await this.SetErrorToResponse(ex);
				return AuthenticateResult.Fail(ex.Error.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return AuthenticateResult.Fail(ex.Message);
			}
		}

		private async Task SetErrorToResponse(ErtisException ex)
		{
			try
			{
				if (!this.Context.Response.HasStarted)
				{
					this.Context.Response.OnStarting(() =>
					{
						this.Context.Response.StatusCode = (int) ex.StatusCode;
						this.Context.Response.ContentType = "application/json";
						var result = Newtonsoft.Json.JsonConvert.SerializeObject(ex.Error);
						return this.Context.Response.WriteAsync(result);
					});
				}
				else
				{
					await this.Context.Response.WriteAsync(ex.Message);
				}
			}
			catch
			{
				await this.Context.Response.WriteAsync(ex.Message);
			}
		}
		
		private async Task<Utilizer> CheckAuthorizationAsync()
		{
			var token = this.Request.GetTokenFromHeader(out var tokenType);
			if (string.IsNullOrEmpty(token))
			{
				throw ErtisAuthException.AuthorizationHeaderMissing();
			}
			
			if (string.IsNullOrEmpty(tokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}
			
			TokenTypeExtensions.TryParseTokenType(tokenType, out var _tokenType);
			switch (_tokenType)
			{
				case SupportedTokenTypes.None:
					throw ErtisAuthException.UnsupportedTokenType();
				case SupportedTokenTypes.Basic:
					var basicToken = new BasicToken(token);
					var applicationId = token.Split(':')[0];
					var getApplicationResponse = await this.applicationService.GetApplicationAsync(applicationId, basicToken);
					if (getApplicationResponse.IsSuccess)
					{
						var rbacDefinition = this.Context.GetRbacDefinition(getApplicationResponse.Data.Id);
						var rbac = $"{rbacDefinition.Resource}.{rbacDefinition.Action}";
						var isPermittedForAction = await this.roleService.CheckPermissionAsync(rbac, basicToken);
						if (!isPermittedForAction)
						{
							throw ErtisAuthException.AccessDenied($"Token owner role is not permitted for this resource/action ({rbac})");
						}
				
						return new Utilizer
						{
							Id = getApplicationResponse.Data.Id,
							Username = getApplicationResponse.Data.Name,
							Type = tokenType == "Basic" ? Utilizer.UtilizerType.Application : Utilizer.UtilizerType.User,
							Role = getApplicationResponse.Data.Role,
							Token = token,
							TokenType = _tokenType
						};
					}
					else
					{
						var errorMessage = getApplicationResponse.Message;
						if (ResponseHelper.TryParseError(getApplicationResponse.Message, out var error))
						{
							errorMessage = error.Message;
						}
						
						throw ErtisAuthException.Unauthorized(errorMessage);
					}
				case SupportedTokenTypes.Bearer:
					var meResponse = await this.authenticationService.WhoAmIAsync(token);
					if (meResponse.IsSuccess)
					{
						var rbacDefinition = this.Context.GetRbacDefinition(meResponse.Data.Id);
						var rbac = $"{rbacDefinition.Resource}.{rbacDefinition.Action}";
						var isPermittedForAction = await this.roleService.CheckPermissionAsync(rbac, BearerToken.CreateTemp(token));
						if (!isPermittedForAction)
						{
							throw ErtisAuthException.AccessDenied($"Token owner role is not permitted for this resource/action ({rbac})");
						}
				
						return new Utilizer
						{
							Id = meResponse.Data.Id,
							Username = meResponse.Data.Username,
							Type = tokenType == "Basic" ? Utilizer.UtilizerType.Application : Utilizer.UtilizerType.User,
							Role = meResponse.Data.Role,
							Token = token,
							TokenType = _tokenType
						};
					}
					else
					{
						var errorMessage = meResponse.Message;
						if (ResponseHelper.TryParseError(meResponse.Message, out var error))
						{
							errorMessage = error.Message;
						}
						
						throw ErtisAuthException.Unauthorized(errorMessage);
					}
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
		}
	}
}