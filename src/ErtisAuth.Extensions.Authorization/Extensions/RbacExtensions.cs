using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ErtisAuth.Extensions.Authorization.Extensions;

public static class RbacExtensions
{
	#region Methods
	
	/// <summary>
	/// Builds the rbac of the endpoint from its rbac attributes. The most specific attribute of each segment applies: an
	/// action's attribute overrides its controller's (the endpoint metadata lists the controller's attributes before the action's),
	/// e.g. [RbacResource("otp")] on an action of a [RbacResource("users")] controller checks the otp resource.
	/// </summary>
	public static Rbac? GetRbacDefinition(this HttpContext httpContext, string utilizerId)
	{
		var endpoint = httpContext.GetEndpoint();
		if (endpoint is RouteEndpoint routeEndpoint)
		{
			// Subject
			var rbacSubjectSegment = string.IsNullOrEmpty(utilizerId) ? RbacSegment.All : new RbacSegment(utilizerId);
			var subjectMetadata = routeEndpoint.Metadata.LastOrDefault(x => x is RbacSubjectAttribute || x.GetType().IsSubclassOf(typeof(RbacSubjectAttribute)));
			if (subjectMetadata is RbacSubjectAttribute rbacSubjectAttribute)
			{
				rbacSubjectSegment = rbacSubjectAttribute.Value;
				if (!string.IsNullOrEmpty(rbacSubjectSegment.Value?.Trim()))
				{
					var rbacSubjectSegmentValue = rbacSubjectSegment.Value.Trim();
					rbacSubjectSegmentValue = RbacSegmentResolver.Resolve(rbacSubjectSegmentValue, httpContext, isSubject: true);
					rbacSubjectSegment = new RbacSegment(rbacSubjectSegmentValue.Replace(".", "%2E"));
				}
			}
			
			// Resource
			var rbacResourceSegment = RbacSegment.All;
			var resourceMetadata = routeEndpoint.Metadata.LastOrDefault(x => x is RbacResourceAttribute || x.GetType().IsSubclassOf(typeof(RbacResourceAttribute)));
			if (resourceMetadata is RbacResourceAttribute rbacResourceAttribute)
			{
				rbacResourceSegment = rbacResourceAttribute.Value;
				if (!string.IsNullOrEmpty(rbacResourceSegment.Value?.Trim()))
				{
					var rbacResourceSegmentValue = rbacResourceSegment.Value.Trim();
					rbacResourceSegmentValue = RbacSegmentResolver.Resolve(rbacResourceSegmentValue, httpContext);
					rbacResourceSegment = new RbacSegment(rbacResourceSegmentValue.Replace(".", "%2E"));
				}
			}
			else
			{
				var routePath = routeEndpoint.RoutePattern.RawText;
				if (!string.IsNullOrEmpty(routePath))
				{
					rbacResourceSegment = new RbacSegment(routePath.Split('/').Last().Replace(".", "%2E"));	
				}
			}
			
			// Action
			var rbacActionSegment = RbacSegment.All;
			var actionMetadata = routeEndpoint.Metadata.LastOrDefault(x => x is RbacActionAttribute || x.GetType().IsSubclassOf(typeof(RbacActionAttribute)));
			if (actionMetadata is RbacActionAttribute rbacActionAttribute)
			{
				rbacActionSegment = rbacActionAttribute.Value;
				if (!string.IsNullOrEmpty(rbacActionSegment.Value?.Trim()))
				{
					var rbacActionSegmentValue = rbacActionSegment.Value.Trim();
					rbacActionSegmentValue = RbacSegmentResolver.Resolve(rbacActionSegmentValue, httpContext);
					rbacActionSegment = new RbacSegment(rbacActionSegmentValue.Replace(".", "%2E"));
				}
			}
			
			// Object
			var rbacObjectSegment = RbacSegment.All;
			var objectMetadata = routeEndpoint.Metadata.LastOrDefault(x => x is RbacObjectAttribute || x.GetType().IsSubclassOf(typeof(RbacObjectAttribute)));
			if (objectMetadata is RbacObjectAttribute rbacObjectAttribute)
			{
				rbacObjectSegment = rbacObjectAttribute.Value;
				if (!string.IsNullOrEmpty(rbacObjectSegment.Value?.Trim()))
				{
					var rbacObjectSegmentValue = rbacObjectSegment.Value.Trim();
					rbacObjectSegmentValue = RbacSegmentResolver.Resolve(rbacObjectSegmentValue, httpContext);
					rbacObjectSegment = new RbacSegment(rbacObjectSegmentValue.Replace(".", "%2E"));
				}
			}
			
			// Rbac
			return new Rbac(rbacSubjectSegment, rbacResourceSegment, rbacActionSegment, rbacObjectSegment);
		}
		
		return null;
	}
	
	#endregion
}