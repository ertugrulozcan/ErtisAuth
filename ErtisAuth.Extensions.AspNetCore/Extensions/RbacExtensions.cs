using System.Linq;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ErtisAuth.Extensions.AspNetCore.Extensions
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
			var resourceMetadata = endpoint?.Metadata?.FirstOrDefault(x => x.GetType() == typeof(RbacResourceAttribute));
			if (resourceMetadata is RbacResourceAttribute rbacResourceAttribute)
			{
				rbacResourceSegment = rbacResourceAttribute.ResourceSegment;
			}
            
			// Action
			var rbacActionSegment = RbacSegment.All;
			var actionMetadata = endpoint?.Metadata?.FirstOrDefault(x => x.GetType() == typeof(RbacActionAttribute));
			if (actionMetadata is RbacActionAttribute rbacActionAttribute)
			{
				rbacActionSegment = rbacActionAttribute.ActionSegment;
			}
            
			// Object
			var rbacObjectSegment = RbacSegment.All;
			var objectMetadata = endpoint?.Metadata?.FirstOrDefault(x => x.GetType() == typeof(RbacObjectAttribute));
			var routeData = httpContext.GetRouteData();
			if (routeData?.Values != null && objectMetadata is RbacObjectAttribute rbacObjectAttribute)
			{
				if (routeData.Values.ContainsKey(rbacObjectAttribute.RouteParameterName))
				{
					var rbacObject = routeData.Values[rbacObjectAttribute.RouteParameterName];
					rbacObjectSegment = new RbacSegment(rbacObject.ToString());
				}
			}

			return new Rbac(rbacSubjectSegment, rbacResourceSegment, rbacActionSegment, rbacObjectSegment);
		}

		#endregion
	}
}