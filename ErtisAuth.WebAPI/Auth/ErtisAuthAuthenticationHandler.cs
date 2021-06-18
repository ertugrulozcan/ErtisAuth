using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Ertis.Core.Exceptions;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Extensions.Http.Extensions;
using ErtisAuth.Infrastructure.Extensions;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErtisAuth.WebAPI.Auth
{
	public class ErtisAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		#region Services

		private readonly ITokenService tokenService;
		private readonly IRoleService roleService;
		
		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options"></param>
		/// <param name="tokenService"></param>
		/// <param name="roleService"></param>
		/// <param name="logger"></param>
		/// <param name="encoder"></param>
		/// <param name="clock"></param>
		public ErtisAuthAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options, 
			ITokenService tokenService, 
			IRoleService roleService,
			ILoggerFactory logger, 
			UrlEncoder encoder, 
			ISystemClock clock) : 
			base(options, logger, encoder, clock)
		{
			this.tokenService = tokenService;
			this.roleService = roleService;
		}

		#endregion
		
		#region Methods

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			try
			{
				var utilizer = await this.CheckAuthorizationAsync();
				
				var identity = new ClaimsIdentity(
					new []
					{
						new Claim(Utilizer.UtilizerIdClaimName, utilizer.Id),
						new Claim(Utilizer.UtilizerTypeClaimName, utilizer.Type.ToString()),
						new Claim(Utilizer.UtilizerUsernameClaimName, utilizer.Username),
						new Claim(Utilizer.UtilizerRoleClaimName, utilizer.Role),
						new Claim(Utilizer.MembershipIdClaimName, utilizer.MembershipId),
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
			var token = this.Context.Request.GetTokenFromHeader(out var tokenType);
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
					var validationResult = await this.tokenService.VerifyBasicTokenAsync(token, false);
					if (!validationResult.IsValidated)
					{
						throw ErtisAuthException.InvalidToken();
					}
					
					var application = validationResult.Application;
					Utilizer applicationUtilizer= application;
					if (!string.IsNullOrEmpty(application.Role))
					{
						var role = await this.roleService.GetByNameAsync(application.Role, application.MembershipId);
						if (role != null)
						{
							var rbac = this.Context.GetRbacDefinition(application.Id);
							if (!role.HasPermission(rbac))
							{
								if (!role.HasOwnUpdatePermission(rbac, applicationUtilizer))
								{
									throw ErtisAuthException.AccessDenied("Your authorization role is unauthorized for this action");
								}
							}
						}
					}
					
					applicationUtilizer.Token = token;
					applicationUtilizer.TokenType = _tokenType;
					return applicationUtilizer;
				case SupportedTokenTypes.Bearer:
					var verifyTokenResult = await this.tokenService.VerifyBearerTokenAsync(token, false);
					if (!verifyTokenResult.IsValidated)
					{
						throw ErtisAuthException.InvalidToken();
					}
        
					var user = verifyTokenResult.User;
					Utilizer userUtilizer= user;
					if (!string.IsNullOrEmpty(user.Role))
					{
						var role = await this.roleService.GetByNameAsync(user.Role, user.MembershipId);
						if (role != null)
						{
							var rbac = this.Context.GetRbacDefinition(user.Id);
							if (!role.HasPermission(rbac))
							{
								if (!role.HasOwnUpdatePermission(rbac, userUtilizer))
								{
									throw ErtisAuthException.AccessDenied("Your authorization role is unauthorized for this action");
								}
							}
						}
					}
					
					userUtilizer.Token = token;
					userUtilizer.TokenType = _tokenType;
					return userUtilizer;
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
		}

		#endregion
	}
}