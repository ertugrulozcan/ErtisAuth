using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.Authorization.Extensions;
using ErtisAuth.Sdk.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Sdk.Middleware;

public class ErtisAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	#region Services
	
	private readonly IAuthorizationHandler<BasicToken> _basicAuthorizationHandler;
	private readonly IAuthorizationHandler<BearerToken> _bearerAuthorizationHandler;
	private readonly ILogger<ErtisAuthAuthenticationHandler> _logger;
	
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
	public ErtisAuthAuthenticationHandler(
		IAuthorizationHandler<BasicToken> basicAuthorizationHandler,
		IAuthorizationHandler<BearerToken> bearerAuthorizationHandler,
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger, 
		UrlEncoder encoder) : 
		base(options, logger, encoder)
	{
		this._basicAuthorizationHandler = basicAuthorizationHandler;
		this._bearerAuthorizationHandler = bearerAuthorizationHandler;
		
		this._logger = logger.CreateLogger<ErtisAuthAuthenticationHandler>();
	}
	
	#endregion
	
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		try
		{
			var isAuthorizedEndpoint = false;
			var isUnauthorizedEndpoint = false;
			var isSelfAuthorizedEndpoint = false;
			
			var endpoint = this.Context.GetEndpoint();
			if (endpoint is RouteEndpoint routeEndpoint)
			{
				var authorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(AuthorizedAttribute));
				var unauthorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(UnauthorizedAttribute));
				var selfAuthorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(SelfAuthorizedAttribute));
				
				if (authorizedAttribute is AuthorizedAttribute)
				{
					isAuthorizedEndpoint = unauthorizedAttribute == null;
				}
				
				if (unauthorizedAttribute is UnauthorizedAttribute)
				{
					isUnauthorizedEndpoint = true;
				}
				
				if (selfAuthorizedAttribute is SelfAuthorizedAttribute)
				{
					isSelfAuthorizedEndpoint = true;
				}
			}
			
			if (!isAuthorizedEndpoint)
			{
				if (isUnauthorizedEndpoint)
				{
					var publicIdentity = new ClaimsIdentity(Array.Empty<Claim>(), null, ClaimExtensions.PublicClaimName, null);
					this.Context.User.AddIdentity(publicIdentity);
					var publicPrincipal = new ClaimsPrincipal(publicIdentity);
					return AuthenticateResult.Success(new AuthenticationTicket(publicPrincipal, this.Scheme.Name));
				}
				else if (isSelfAuthorizedEndpoint)
				{
					var selfIdentity = await this.GetClaimsIdentityAsync(passAuthorization: true);
					this.Context.User.AddIdentity(selfIdentity);
					var selfPrincipal = new ClaimsPrincipal(selfIdentity);
					return AuthenticateResult.Success(new AuthenticationTicket(selfPrincipal, this.Scheme.Name));
				}
				else
				{
					return AuthenticateResult.NoResult();	
				}
			}
			
			var identity = await this.GetClaimsIdentityAsync();
			this.Context.User.AddIdentity(identity);
			var principal = new ClaimsPrincipal(identity);
			return AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name));
		}
		catch (ErtisAuthException ex)
		{
			try
			{
				if (!this.Context.Response.HasStarted)
				{
					this.Context.Response.OnStarting(() =>
					{
						this.Context.Response.StatusCode = (int) ex.StatusCode;
						this.Context.Response.ContentType = "application/json";
						var result = JsonSerializer.Serialize(ex.Error);
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
			
			return AuthenticateResult.Fail(ex.Error.Message);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "ErtisAuthAuthenticationHandler.HandleAuthenticateAsync occured an error");
			return AuthenticateResult.Fail(ex.Message);
		}
	}
	
	private async Task<ClaimsIdentity> GetClaimsIdentityAsync(bool passAuthorization = false)
	{
		var utilizer = passAuthorization ? await this.CheckAuthenticationAsync() : await this.CheckAuthorizationAsync();
		return utilizer.ToClaimsIdentity();
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
	
	private async Task<Utilizer> CheckAuthenticationAsync()
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
				return await this._basicAuthorizationHandler.CheckAuthenticationAsync(basicToken);
			}
			case SupportedTokenTypes.Bearer:
			{
				var bearerToken = BearerToken.CreateTemp(token);
				return await this._bearerAuthorizationHandler.CheckAuthenticationAsync(bearerToken);
			}
			default:
				throw ErtisAuthException.UnsupportedTokenType();
		}
	}
}