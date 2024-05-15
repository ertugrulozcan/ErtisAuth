using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Ertis.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.Authorization.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Extensions.AspNetCore
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class ErtisAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		#region Services

		private readonly IAuthorizationHandler<BasicToken> _basicAuthorizationHandler;
		private readonly IAuthorizationHandler<BearerToken> _bearerAuthorizationHandler;

		#endregion
		
		#region Constructors
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="basicAuthorizationHandler"></param>
		/// <param name="bearerAuthorizationHandler"></param>
		/// <param name="options"></param>
		/// <param name="logger"></param>
		/// <param name="encoder"></param>
		/// <param name="clock"></param>
		public ErtisAuthAuthenticationHandler(
			IAuthorizationHandler<BasicToken> basicAuthorizationHandler,
			IAuthorizationHandler<BearerToken> bearerAuthorizationHandler,
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger, 
			UrlEncoder encoder, 
			ISystemClock clock) : 
			base(options, logger, encoder, clock)
		{
			this._basicAuthorizationHandler = basicAuthorizationHandler;
			this._bearerAuthorizationHandler = bearerAuthorizationHandler;
		}
		
		#endregion

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			try
			{
				var isAuthorizedEndpoint = false;
				var endpoint = this.Context.GetEndpoint();
				if (endpoint is RouteEndpoint routeEndpoint)
				{
					var authorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(AuthorizedAttribute));
					var unauthorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(UnauthorizedAttribute));
					if (authorizedAttribute is AuthorizedAttribute)
					{
						isAuthorizedEndpoint = unauthorizedAttribute == null;
					}
				}

				if (!isAuthorizedEndpoint)
				{
					return AuthenticateResult.NoResult();
				}
				
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
			var token = this.Request.GetTokenFromHeader(out var tokenType);
			if (string.IsNullOrEmpty(token))
			{
				throw ErtisAuthException.AuthorizationHeaderMissing();
			}
			
			if (string.IsNullOrEmpty(tokenType) || !TokenTypeExtensions.TryParseTokenType(tokenType, out var _tokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}
			
			switch (_tokenType)
			{
				case SupportedTokenTypes.None:
					throw ErtisAuthException.UnsupportedTokenType();
				case SupportedTokenTypes.Basic:
				{
					var basicToken = new BasicToken(token);
					var authorizationResult = await this._basicAuthorizationHandler.CheckAuthorizationAsync(basicToken, this.Context);
					if (authorizationResult.IsAuthorized)
					{
						return authorizationResult.Utilizer;
					}
					else
					{
						throw ErtisAuthException.AccessDenied($"You don't have permission to perform this action. Rbac: {authorizationResult.Rbac} (Error Code: 4031)");	
					}
				}
				case SupportedTokenTypes.Bearer:
				{
					var bearerToken = BearerToken.CreateTemp(token);
					var authorizationResult = await this._bearerAuthorizationHandler.CheckAuthorizationAsync(bearerToken, this.Context);
					if (authorizationResult.IsAuthorized)
					{
						return authorizationResult.Utilizer;
					}
					else
					{
						throw ErtisAuthException.AccessDenied($"You don't have permission to perform this action. Rbac: {authorizationResult.Rbac} (Error Code: 4032)");	
					}
				}
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
		}
	}
}