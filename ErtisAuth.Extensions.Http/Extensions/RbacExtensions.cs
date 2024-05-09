using System;
using System.Linq;
using Ertis.TemplateEngine;
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
			var formatter = new Formatter(new ParserOptions { OpenBrackets = "{", CloseBrackets = "}" });
			
			if (endpoint is RouteEndpoint routeEndpoint)
			{
				// Subject
				var rbacSubjectSegment = string.IsNullOrEmpty(utilizerId) ? RbacSegment.All : new RbacSegment(utilizerId);
				var subjectMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacSubjectAttribute) || x.GetType().IsSubclassOf(typeof(RbacSubjectAttribute)));
				if (subjectMetadata is RbacSubjectAttribute rbacSubjectAttribute)
				{
					rbacSubjectSegment = rbacSubjectAttribute.Value;
					if (!string.IsNullOrEmpty(rbacSubjectSegment.Value?.Trim()))
					{
						var rbacSubjectSegmentValue = rbacSubjectSegment.Value.Trim();
						rbacSubjectSegmentValue = SetEnvironmentVariablesToSegment(rbacSubjectSegmentValue, formatter);
						rbacSubjectSegmentValue = formatter.Format(rbacSubjectSegmentValue, httpContext.Request.RouteValues);
						rbacSubjectSegment = new RbacSegment(rbacSubjectSegmentValue);
					}
				}
				
				// Resource
				var rbacResourceSegment = RbacSegment.All;
				var resourceMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacResourceAttribute) || x.GetType().IsSubclassOf(typeof(RbacResourceAttribute)));
				if (resourceMetadata is RbacResourceAttribute rbacResourceAttribute)
				{
					rbacResourceSegment = rbacResourceAttribute.Value;
					if (!string.IsNullOrEmpty(rbacResourceSegment.Value?.Trim()))
					{
						var rbacResourceSegmentValue = rbacResourceSegment.Value.Trim();
						rbacResourceSegmentValue = SetEnvironmentVariablesToSegment(rbacResourceSegmentValue, formatter);
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
				var actionMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacActionAttribute) || x.GetType().IsSubclassOf(typeof(RbacActionAttribute)));
				if (actionMetadata is RbacActionAttribute rbacActionAttribute)
				{
					rbacActionSegment = rbacActionAttribute.Value;
					if (!string.IsNullOrEmpty(rbacActionSegment.Value?.Trim()))
					{
						var rbacActionSegmentValue = rbacActionSegment.Value.Trim();
						rbacActionSegmentValue = SetEnvironmentVariablesToSegment(rbacActionSegmentValue, formatter);
						rbacActionSegmentValue = formatter.Format(rbacActionSegmentValue, httpContext.Request.RouteValues);
						rbacActionSegment = new RbacSegment(rbacActionSegmentValue);
					}
				}
				
				// Object
				var rbacObjectSegment = RbacSegment.All;
				var objectMetadata = routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacObjectAttribute) || x.GetType().IsSubclassOf(typeof(RbacObjectAttribute)));
				if (objectMetadata is RbacObjectAttribute rbacObjectAttribute)
				{
					rbacObjectSegment = rbacObjectAttribute.Value;
					if (!string.IsNullOrEmpty(rbacObjectSegment.Value?.Trim()))
					{
						var rbacObjectSegmentValue = rbacObjectSegment.Value.Trim();
						rbacObjectSegmentValue = SetEnvironmentVariablesToSegment(rbacObjectSegmentValue, formatter);
						rbacObjectSegmentValue = formatter.Format(rbacObjectSegmentValue, httpContext.Request.RouteValues);
						rbacObjectSegment = new RbacSegment(rbacObjectSegmentValue);
					}
				}

				// Rbac
				return new Rbac(rbacSubjectSegment, rbacResourceSegment, rbacActionSegment, rbacObjectSegment);
			}

			return default;
		}
		
		private static string SetEnvironmentVariablesToSegment(string segmentValue, Formatter formatter)
		{
			// ReSharper disable once InconsistentNaming
			const string PREFIX = "env::";
			
			if (string.IsNullOrEmpty(segmentValue) || !segmentValue.Contains(PREFIX))
			{
				return segmentValue;
			}
			
			var segments = formatter.LookUp(segmentValue);
			foreach (var segment in segments)
			{
				if (segment.Type == SegmentType.PlaceHolder && segment is PlaceHolder placeHolder && placeHolder.Inner.StartsWith(PREFIX))
				{
					var environmentVariable = placeHolder.Inner[PREFIX.Length..];
					var value = Environment.GetEnvironmentVariable(environmentVariable);
					segmentValue = segmentValue.Replace(placeHolder.Outer, value);
				}
			}

			return segmentValue;
		}
		
		#endregion
	}
}