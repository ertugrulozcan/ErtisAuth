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
	/// The most specific of [Authorized], [SelfAuthorized] and [Unauthorized] decides: an action's attribute overrides its
	/// controller's (the endpoint metadata lists the controller's attributes before the action's),
	/// e.g. an [Unauthorized] action of an [Authorized] controller is public, a [SelfAuthorized] action of an
	/// [Unauthorized] controller is self authorized.
	/// </summary>
	public static EndpointAuthorization GetEndpointAuthorization(this HttpContext httpContext)
	{
		if (httpContext.GetEndpoint() is not RouteEndpoint routeEndpoint)
		{
			return EndpointAuthorization.None;
		}

		var mostSpecific = routeEndpoint.Metadata.LastOrDefault(x =>
			x.GetType() == typeof(AuthorizedAttribute) ||
			x.GetType() == typeof(SelfAuthorizedAttribute) ||
			x.GetType() == typeof(UnauthorizedAttribute));

		return mostSpecific switch
		{
			AuthorizedAttribute => EndpointAuthorization.Authorized,
			SelfAuthorizedAttribute => EndpointAuthorization.SelfAuthorized,
			UnauthorizedAttribute => EndpointAuthorization.Public,
			_ => EndpointAuthorization.None
		};
	}
	
	#endregion
}