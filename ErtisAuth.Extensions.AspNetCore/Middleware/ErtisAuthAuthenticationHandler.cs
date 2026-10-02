using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Authentication;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.Authorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Extensions.AspNetCore.Middleware;

public class ErtisAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	#region Constants
	
	private const string AuthenticationErrorItemKey = "ErtisAuth.AuthenticationError";
	
	#endregion
	
	#region Services
	
	private readonly ITokenService tokenService;
	private readonly IRoleService roleService;
	private readonly IAccessControlService accessControlService;
	
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
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Writes the authentication error (status code and error body) of the request; 401 responses get the WWW-Authenticate challenge.
	/// </summary>
	protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		if (this.Context.Items.TryGetValue(AuthenticationErrorItemKey, out var item) && item is ErtisAuthException ex && !this.Response.HasStarted)
		{
			this.Response.StatusCode = (int) ex.StatusCode;
			if (this.Response.StatusCode == StatusCodes.Status401Unauthorized)
			{
				this.Response.Headers.WWWAuthenticate = Authorization.Scheme.WwwAuthenticate;
			}
			
			this.Response.ContentType = "application/json";
			await this.Response.WriteAsync(JsonSerializer.Serialize(ex.Error));
			return;
		}
		
		await base.HandleChallengeAsync(properties);
		this.Response.Headers.WWWAuthenticate = Authorization.Scheme.WwwAuthenticate;
	}
	
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		try
		{
			var endpoint = this.Context.GetEndpoint();
			var endpointAuthorization = this.Context.GetEndpointAuthorization();
			if (endpointAuthorization == EndpointAuthorization.Public)
			{
				var publicIdentity = new ClaimsIdentity(Array.Empty<Claim>(), null, ClaimExtensions.PublicClaimName, null);
				this.Context.User.AddIdentity(publicIdentity);
				var publicPrincipal = new ClaimsPrincipal(publicIdentity);
				return AuthenticateResult.Success(new AuthenticationTicket(publicPrincipal, this.Scheme.Name));
			}
			
			if (endpointAuthorization == EndpointAuthorization.None)
			{
				return AuthenticateResult.NoResult();
			}
			
			// Self authorized endpoints check the permission themselves
			var utilizer = await this.CheckAuthorizationAsync(checkPermission: endpointAuthorization == EndpointAuthorization.Authorized);
			this.CheckMembershipScope(endpoint, utilizer);
			
			var identity = utilizer.ToClaimsIdentity();
			this.Context.User.AddIdentity(identity);
			var principal = new ClaimsPrincipal(identity);
			
			return AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name));
		}
		catch (ErtisAuthException ex)
		{
			// Written by HandleChallengeAsync: the ErtisAuth policy challenges this scheme for the failed authentication
			this.Context.Items[AuthenticationErrorItemKey] = ex;
			return AuthenticateResult.Fail(ex.Error.Message);
		}
		
		// Any other exception (e.g. the database is unreachable) is not an authentication failure: it reaches the
		// global exception handler (500), so that clients don't take an outage for an invalid token
	}
	
	private async Task<Utilizer> CheckAuthorizationAsync(bool checkPermission)
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
				if (checkPermission && !string.IsNullOrEmpty(application.Role))
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
				
				// Before the permission check: the token scopes are its final gate
				var scopes = verifyTokenResult.Scopes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
				userUtilizer.Scopes = scopes is { Length: > 0 } ? scopes : null;
				
				if (checkPermission && !string.IsNullOrEmpty(user.Role))
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
				
				return userUtilizer;
			default:
				throw ErtisAuthException.UnsupportedTokenType();
		}
	}
	
	/// <summary>
	/// Membership bounded endpoints (routed with MembershipRouteAttribute) are only accessible with a token of the same membership.
	/// </summary>
	private void CheckMembershipScope(Endpoint? endpoint, Utilizer utilizer)
	{
		if (endpoint?.Metadata.GetMetadata<MembershipRouteAttribute>() == null)
		{
			return;
		}
		
		var routeMembershipId = this.Context.Request.RouteValues[MembershipRouteAttribute.ParameterName] as string;
		if (string.IsNullOrEmpty(routeMembershipId) || routeMembershipId != utilizer.MembershipId)
		{
			throw ErtisAuthException.AccessDenied("You do not have access to the resources of this membership");
		}
	}
	
	#endregion
}