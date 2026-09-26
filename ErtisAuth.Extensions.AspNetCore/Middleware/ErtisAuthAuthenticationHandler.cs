using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.Authorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Extensions.AspNetCore.Middleware;

public class ErtisAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	#region Services
	
	private readonly ITokenService tokenService;
	private readonly IRoleService roleService;
	private readonly IAccessControlService accessControlService;
	private readonly ILogger<ErtisAuthAuthenticationHandler> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="options"></param>
	/// <param name="tokenService"></param>
	/// <param name="roleService"></param>
	/// <param name="accessControlService"></param>
	/// <param name="logger"></param>
	/// <param name="encoder"></param>
	public ErtisAuthAuthenticationHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options, 
		ITokenService tokenService, 
		IRoleService roleService,
		IAccessControlService accessControlService,
		ILoggerFactory logger, 
		UrlEncoder encoder) : 
		base(options, logger, encoder)
	{
		this.tokenService = tokenService;
		this.roleService = roleService;
		this.accessControlService = accessControlService;
		
		this._logger = logger.CreateLogger<ErtisAuthAuthenticationHandler>();
	}
	
	#endregion
	
	#region Methods
	
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		try
		{
			var isAuthorizedEndpoint = false;
			var isUnauthorizedEndpoint = false;
			
			var endpoint = this.Context.GetEndpoint();
			if (endpoint is RouteEndpoint routeEndpoint)
			{
				var authorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(AuthorizedAttribute));
				var unauthorizedAttribute = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(UnauthorizedAttribute));
				if (authorizedAttribute is AuthorizedAttribute)
				{
					isAuthorizedEndpoint = unauthorizedAttribute == null;
				}
				
				if (unauthorizedAttribute is UnauthorizedAttribute)
				{
					isUnauthorizedEndpoint = true;
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
				else
				{
					return AuthenticateResult.NoResult();
				}
			}
			
			var utilizer = await this.CheckAuthorizationAsync();
			var identity = utilizer.ToClaimsIdentity();
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
					this.Context.Response.OnStarting(async () =>
					{
						this.Context.Response.StatusCode = (int) ex.StatusCode;
						this.Context.Response.ContentType = "application/json";
						var result = JsonSerializer.Serialize(ex.Error);
						await this.Context.Response.WriteAsync(result);
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
			this._logger.LogError(ex, "ErtisAuthAuthenticationHandler.HandleAuthenticateAsync occurred an error");
			return AuthenticateResult.Fail(ex.Message);
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
					var role = await this.roleService.GetBySlugAsync(application.Role, application.MembershipId);
					if (role != null)
					{
						var rbac = this.Context.GetRbacDefinition(application.Id);
						if (rbac == null)
						{
							throw ErtisAuthException.AccessDenied("Rbac definition not found");
						}
						
						if (!this.accessControlService.HasPermission(role, rbac, applicationUtilizer))
						{
							throw ErtisAuthException.AccessDenied($"Your authorization role ({role.Slug}) is unauthorized for this action ({rbac})");	
						}
					}
					else
					{
						throw ErtisAuthException.AccessDenied($"The application role is not found by the given slug: '{application.Role}'");
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

				if (verifyTokenResult.IsRefreshToken)
				{
					throw ErtisAuthException.InvalidToken("Refresh tokens can not be used as access tokens");
				}

				var user = verifyTokenResult.User;
				if (user == null)
				{
					throw ErtisAuthException.AccessDenied("User not found");
				}
				
				Utilizer userUtilizer= user;
				if (!string.IsNullOrEmpty(user.Role))
				{
					var role = await this.roleService.GetBySlugAsync(user.Role, user.MembershipId);
					if (role != null)
					{
						var rbac = this.Context.GetRbacDefinition(user.Id);
						if (rbac == null)
						{
							throw ErtisAuthException.AccessDenied("Rbac definition not found");
						}
						
						if (!this.accessControlService.HasPermission(role, rbac, userUtilizer))
						{
							throw ErtisAuthException.AccessDenied($"Your authorization role ({role.Slug}) is unauthorized for this action ({rbac})");		
						}
					}
					else
					{
						throw ErtisAuthException.AccessDenied($"The user role is not found by the given slug: '{user.Role}'");
					}
				}
				
				userUtilizer.Token = token;
				userUtilizer.TokenType = _tokenType;
				
				var scopes = verifyTokenResult.Scopes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				userUtilizer.Scopes = scopes is { Length: > 0 } ? scopes : null;
				
				return userUtilizer;
			default:
				throw ErtisAuthException.UnsupportedTokenType();
		}
	}
	
	#endregion
}