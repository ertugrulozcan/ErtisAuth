using System.Linq;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ErtisAuth.Extensions.Http.Extensions
{
	public static class RbacExtensions
	{
		#region Methods

		public static Rbac GetRbacDefinition(this HttpContext httpContext, string subjectId)
		{
			var endpoint = httpContext.GetEndpoint();
                    		
			// Subject
			var rbacSubjectSegment = new RbacSegment(subjectId);
            
			// Resource
			var rbacResourceSegment = RbacSegment.All;
			var resourceMetadata = endpoint?.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacResourceAttribute));
			if (resourceMetadata is RbacResourceAttribute rbacResourceAttribute)
			{
				rbacResourceSegment = rbacResourceAttribute.ResourceSegment;
			}
			else if (endpoint is RouteEndpoint routeEndpoint)
			{
				var routePath = routeEndpoint.RoutePattern.RawText;
				if (!string.IsNullOrEmpty(routePath))
				{
					rbacResourceSegment = new RbacSegment(routePath.Split('/').Last());	
				}
			}
            
			// Action
			var rbacActionSegment = RbacSegment.All;
			var actionMetadata = endpoint?.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacActionAttribute));
			if (actionMetadata is RbacActionAttribute rbacActionAttribute)
			{
				rbacActionSegment = rbacActionAttribute.ActionSegment;
			}
            
			// Object
			var rbacObjectSegment = RbacSegment.All;
			var objectMetadata = endpoint?.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacObjectAttribute));
			var routeData = httpContext.GetRouteData();
			if (objectMetadata is RbacObjectAttribute rbacObjectAttribute)
			{
				if (routeData.Values.ContainsKey(rbacObjectAttribute.ObjectSegment.Value))
				{
					var rbacObject = routeData.Values[rbacObjectAttribute.ObjectSegment.Value];
					if (rbacObject != null)
					{
						rbacObjectSegment = new RbacSegment(rbacObject.ToString());	
					}
				}
			}

			return new Rbac(rbacSubjectSegment, rbacResourceSegment, rbacActionSegment, rbacObjectSegment);
		}

		#endregion
	}
}