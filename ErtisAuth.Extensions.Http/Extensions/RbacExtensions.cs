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

		public static Rbac GetRbacDefinition(this HttpContext httpContext, string utilizerId)
		{
			var endpoint = httpContext.GetEndpoint();
			var formatter = new Ertis.TemplateEngine.Formatter(new Ertis.TemplateEngine.ParserOptions { OpenBrackets = "{", CloseBrackets = "}" });
			
			if (endpoint is RouteEndpoint routeEndpoint)
			{
				// Subject
				var rbacSubjectSegment = string.IsNullOrEmpty(utilizerId) ? RbacSegment.All : new RbacSegment(utilizerId);
				var subjectMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacSubjectAttribute));
				if (subjectMetadata is RbacSubjectAttribute rbacSubjectAttribute)
				{
					rbacSubjectSegment = rbacSubjectAttribute.Value;
					if (!string.IsNullOrEmpty(rbacSubjectSegment.Value?.Trim()))
					{
						var rbacSubjectSegmentValue = rbacSubjectSegment.Value.Trim();
						rbacSubjectSegmentValue = formatter.Format(rbacSubjectSegmentValue, httpContext.Request.RouteValues);
						rbacSubjectSegment = new RbacSegment(rbacSubjectSegmentValue);
					}
				}
				
				// Resource
				var rbacResourceSegment = RbacSegment.All;
				var resourceMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacResourceAttribute));
				if (resourceMetadata is RbacResourceAttribute rbacResourceAttribute)
				{
					rbacResourceSegment = rbacResourceAttribute.Value;
					if (!string.IsNullOrEmpty(rbacResourceSegment.Value?.Trim()))
					{
						var rbacResourceSegmentValue = rbacResourceSegment.Value.Trim();
						rbacResourceSegmentValue = formatter.Format(rbacResourceSegmentValue, httpContext.Request.RouteValues);
						rbacResourceSegment = new RbacSegment(rbacResourceSegmentValue);
					}
				}
				else
				{
					var routePath = routeEndpoint.RoutePattern.RawText;
					if (!string.IsNullOrEmpty(routePath))
					{
						rbacResourceSegment = new RbacSegment(routePath.Split('/').Last());	
					}
				}

				// Action
				var rbacActionSegment = RbacSegment.All;
				var actionMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacActionAttribute));
				if (actionMetadata is RbacActionAttribute rbacActionAttribute)
				{
					rbacActionSegment = rbacActionAttribute.Value;
					if (!string.IsNullOrEmpty(rbacActionSegment.Value?.Trim()))
					{
						var rbacActionSegmentValue = rbacActionSegment.Value.Trim();
						rbacActionSegmentValue = formatter.Format(rbacActionSegmentValue, httpContext.Request.RouteValues);
						rbacActionSegment = new RbacSegment(rbacActionSegmentValue);
					}
				}
				
				// Object
				var rbacObjectSegment = RbacSegment.All;
				var objectMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacObjectAttribute));
				if (objectMetadata is RbacObjectAttribute rbacObjectAttribute)
				{
					rbacObjectSegment = rbacObjectAttribute.Value;
					if (!string.IsNullOrEmpty(rbacObjectSegment.Value?.Trim()))
					{
						var rbacObjectSegmentValue = rbacObjectSegment.Value.Trim();
						rbacObjectSegmentValue = formatter.Format(rbacObjectSegmentValue, httpContext.Request.RouteValues);
						rbacObjectSegment = new RbacSegment(rbacObjectSegmentValue);
					}
				}

				// Rbac
				return new Rbac(rbacSubjectSegment, rbacResourceSegment, rbacActionSegment, rbacObjectSegment);
			}

			return default;
		}

		#endregion
	}
}