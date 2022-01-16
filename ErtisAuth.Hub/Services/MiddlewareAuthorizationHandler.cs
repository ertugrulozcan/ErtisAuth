using System;
using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.AspNetCore;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels.Auth;

namespace ErtisAuth.Hub.Services
{
    public class MiddlewareAuthorizationHandler : AuthorizationHandler<ErtisAuthAuthorizationRequirement>
	{
		#region Services

		private readonly IAuthenticationService authenticationService;
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly IRoleService roleService;
		
		#endregion

		#region Properties

		private string[] PrivilegedAccessEndpoints { get; } = 
		{
			"/api/auth/refresh-token"
		};

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="authenticationService"></param>
		/// <param name="roleService"></param>
		/// <param name="httpContextAccessor"></param>
		public MiddlewareAuthorizationHandler(IAuthenticationService authenticationService, IRoleService roleService, IHttpContextAccessor httpContextAccessor)
		{
			this.authenticationService = authenticationService;
			this.roleService = roleService;
			this.httpContextAccessor = httpContextAccessor;
		}

		#endregion
		
		#region Methods
		
		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ErtisAuthAuthorizationRequirement requirement)
		{
			try
			{
				var httpContext = httpContextAccessor.HttpContext;
				if (httpContext == null)
				{
					context.Fail();
					return;
				}
				
				if (this.PrivilegedAccessEndpoints.Contains(httpContext.Request.Path.Value))
				{
					context.Succeed(requirement);
					return;
				}

				var accessToken = context.GetAccessToken();
				var response = await this.authenticationService.VerifyTokenAsync(accessToken);
				if (response.IsSuccess)
				{
					if (TryExtractEndpointRbacFromAuthorizationContext(context, out var rbac))
					{
						var isPermitted = await this.roleService.CheckPermissionAsync(rbac.ToString(), BearerToken.CreateTemp(context.GetAccessToken()));
						if (!isPermitted)
						{
							httpContext.Response.StatusCode = 403;
							httpContext.Response.Headers.Add("Authorization", context.GetAccessToken());
							httpContext.Response.Redirect($"/unauthorized");
							ForbiddenRbacTrace.Push(httpContext.GetClaim(Claims.UserId), rbac.ToString());

							return;
						}
					}
					
					context.Succeed(requirement);
					SetSessionTime(httpContext, response);
				}
				else
				{
					foreach (var cookie in httpContext.Request.Cookies.Keys)
					{
						httpContext.Response.Cookies.Delete(cookie);
					}
					
					context.Fail();	
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				context.Fail();	
			}
		}
		
		private static bool TryExtractEndpointRbacFromAuthorizationContext(AuthorizationHandlerContext context, out Rbac rbac)
		{
			if (context.Resource is Microsoft.AspNetCore.Routing.RouteEndpoint routeEndpoint)
			{
				// Subject
				var rbacSubjectSegment = new RbacSegment(context.GetClaim(Claims.UserId));
					
				// Resource
				string slug = null;
				if (routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacResourceAttribute)) is RbacResourceAttribute rbacResourceAttribute)
				{
					slug = rbacResourceAttribute.ResourceSegment.Slug;
				}
				else if (TryExtractFirstSlugSegmentFromRouteEndpoint(routeEndpoint, out var routePatternPartSlug))
				{
					slug = routePatternPartSlug;
				}

				// Action
				var rbacActionSegment = Rbac.GetSegment(Rbac.CrudActions.Read);
				if (routeEndpoint.Metadata.FirstOrDefault(x => x.GetType() == typeof(RbacActionAttribute)) is RbacActionAttribute rbacActionAttribute)
				{
					rbacActionSegment = rbacActionAttribute.ActionSegment;
				}
				
				// Object
				var rbacObjectSegment = RbacSegment.All;
				
				if (!string.IsNullOrEmpty(slug))
				{
					rbac = new Rbac(rbacSubjectSegment, new RbacSegment(slug), rbacActionSegment, rbacObjectSegment);
					return true;
				}
			}

			rbac = default;
			return false;
		}

		private static bool TryExtractFirstSlugSegmentFromRouteEndpoint(Microsoft.AspNetCore.Routing.RouteEndpoint routeEndpoint, out string slug)
		{
			var pathSegments = routeEndpoint.RoutePattern.PathSegments;
			foreach (var pathSegment in pathSegments)
			{
				if (pathSegment.Parts.FirstOrDefault() is RoutePatternLiteralPart routePatternPart)
				{
					// Ignore bff-api requests
					if (routePatternPart.Content == "api")
					{
						continue;
					}

					slug = routePatternPart.Content;
					return true;
				}
			}

			slug = null;
			return false;
		}

		private static void SetSessionTime(HttpContext httpContext, IResponseResult<ITokenValidationResult> response)
		{
			if (httpContext.Items.ContainsKey("TokenValidationTime"))
			{
				httpContext.Items["TokenValidationTime"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
			}
			else
			{
				httpContext.Items.Add("TokenValidationTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));	
			}

			if (response.Data is BearerTokenValidationResult bearerTokenValidationResult)
			{
				var totalRemainingMilliseconds = bearerTokenValidationResult.RemainingTime.TotalMilliseconds;
				if (httpContext.Items.ContainsKey("RemainingTime"))
				{
					httpContext.Items["RemainingTime"] = totalRemainingMilliseconds;
				}
				else
				{
					httpContext.Items.Add("RemainingTime", totalRemainingMilliseconds);	
				}
			}
		}

		#endregion
	}
}