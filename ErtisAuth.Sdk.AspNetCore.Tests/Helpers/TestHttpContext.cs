using ErtisAuth.Core.Attributes;
using ErtisAuth.Core.Models.Roles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace ErtisAuth.Sdk.AspNetCore.Tests.Helpers;

/// <summary>
/// An HttpContext for a request to an endpoint of the client application, without running a server.
/// </summary>
internal static class TestHttpContext
{
	#region Methods
	
	/// <summary>
	/// A request to 'users/{id}' with the given endpoint metadata and authorization header.
	/// The rbac attributes make the endpoint resolve to [utilizer].users.read.[id].
	/// </summary>
	public static DefaultHttpContext Create(string? authorizationHeader = null, params object[] metadata)
	{
		var httpContext = new DefaultHttpContext();
		httpContext.Response.Body = new MemoryStream();
		
		if (authorizationHeader != null)
		{
			httpContext.Request.Headers.Authorization = authorizationHeader;
		}
		
		httpContext.Request.RouteValues["id"] = "user-1";
		
		object[] rbacMetadata =
		[
			new RbacResourceAttribute("users"),
			new RbacActionAttribute(Rbac.CrudActions.Read),
			new RbacObjectAttribute("{id}")
		];
		
		var endpoint = new RouteEndpoint(
			_ => Task.CompletedTask,
			RoutePatternFactory.Parse("users/{id}"),
			0,
			new EndpointMetadataCollection(rbacMetadata.Concat(metadata)),
			"test-endpoint");
		
		httpContext.SetEndpoint(endpoint);
		return httpContext;
	}
	
	#endregion
}
