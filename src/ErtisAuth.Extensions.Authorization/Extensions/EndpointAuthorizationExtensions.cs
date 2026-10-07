using ErtisAuth.Extensions.Authorization.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ErtisAuth.Extensions.Authorization.Extensions;

/// <summary>
/// How an endpoint is authorized by the ErtisAuth authentication handlers (the API's and ErtisAuth.Sdk.AspNetCore's).
/// </summary>
public enum EndpointAuthorization
{
	/// <summary>
	/// Not an ErtisAuth endpoint: the handler does not authenticate the request.
	/// </summary>
	None,
	
	/// <summary>
	/// [Unauthorized]: public, no token required.
	/// </summary>
	Public,
	
	/// <summary>
	/// [Authorized]: the handler authenticates the token and checks the endpoint's rbac permission.
	/// </summary>
	Authorized,
	
	/// <summary>
	/// [SelfAuthorized]: the handler authenticates the token (valid, not revoked, not a refresh token, of the route's membership),
	/// the endpoint checks the permission itself (e.g. with an rbac it can only build from the requested data).
	/// </summary>
	SelfAuthorized
}

public static class EndpointAuthorizationExtensions
{
	#region Methods
	
	/// <summary>
	/// [Unauthorized] on the controller or on the action makes the endpoint public.
	/// Otherwise the most specific of [Authorized] and [SelfAuthorized] decides: an action's attribute overrides its
	/// controller's (the endpoint metadata lists the controller's attributes before the action's),
	/// e.g. a [SelfAuthorized] action of an [Authorized] controller is self authorized.
	/// </summary>
	public static EndpointAuthorization GetEndpointAuthorization(this HttpContext httpContext)
	{
		if (httpContext.GetEndpoint() is not RouteEndpoint routeEndpoint)
		{
			return EndpointAuthorization.None;
		}
		
		if (routeEndpoint.Metadata.Any(x => x.GetType() == typeof(UnauthorizedAttribute)))
		{
			return EndpointAuthorization.Public;
		}
		
		var mostSpecific = routeEndpoint.Metadata.LastOrDefault(x => x.GetType() == typeof(AuthorizedAttribute) || x.GetType() == typeof(SelfAuthorizedAttribute));
		return mostSpecific switch
		{
			AuthorizedAttribute => EndpointAuthorization.Authorized,
			SelfAuthorizedAttribute => EndpointAuthorization.SelfAuthorized,
			_ => EndpointAuthorization.None
		};
	}
	
	#endregion
}