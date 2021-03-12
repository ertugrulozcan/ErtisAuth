using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Extensions;
using ErtisAuth.WebAPI.Annotations;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ErtisAuth.WebAPI.Services
{
	public class ErtisAuthAuthorizationRequirement : IAuthorizationRequirement
	{
		
	}
	
	public class ErtisAuthAuthorizationHandler : AuthorizationHandler<ErtisAuthAuthorizationRequirement>
	{
		#region Services

		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly ITokenService tokenService;
		private readonly IRoleService roleService;
		
		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="httpContextAccessor"></param>
		/// <param name="tokenService"></param>
		/// <param name="roleService"></param>
		public ErtisAuthAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IRoleService roleService)
		{
			this.httpContextAccessor = httpContextAccessor;
			this.tokenService = tokenService;
			this.roleService = roleService;
		}

		#endregion
		
		#region Methods

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ErtisAuthAuthorizationRequirement requirement)
		{
			var httpContext = httpContextAccessor.HttpContext;
			
			try
			{
				var utilizer = await this.CheckAuthorizationAsync(httpContext);
				
				var identity = new ClaimsIdentity(
					new []
					{
						new Claim(Utilizer.UtilizerIdClaimName, utilizer.Id),
						new Claim(Utilizer.UtilizerTypeClaimName, utilizer.Type.ToString().ToLower()),
						new Claim(Utilizer.UtilizerRoleClaimName, utilizer.Role),
						new Claim(Utilizer.MembershipIdClaimName, utilizer.MembershipId)
					}, 
					null, 
					"Utilizer", 
					utilizer.Role);
					
				context.User.AddIdentity(identity);
				
				context.Succeed(requirement);
			}
			catch (ErtisAuthException ex)
			{
				httpContext.Response.StatusCode = ex.Error.StatusCode;
				
				byte[] data = System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(ex.Error));
				httpContext.Response.ContentType = "application/json";
				await httpContext.Response.Body.WriteAsync(data, 0, data.Length);
				
				context.Succeed(requirement);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				context.Fail();	
			}
		}

		private async Task<Utilizer> CheckAuthorizationAsync(HttpContext httpContext)
		{
			var token = httpContext.Request.GetTokenFromHeader(out var tokenType);
			if (string.IsNullOrEmpty(token))
			{
				throw ErtisAuthException.AuthorizationHeaderMissing();
			}
			
			if (string.IsNullOrEmpty(tokenType))
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}

			switch (tokenType)
			{
				case "Bearer":
				{
					var verifyTokenResult = await this.tokenService.VerifyBearerTokenAsync(token, false);
                    if (!verifyTokenResult.IsValidated)
                    {
                    	throw ErtisAuthException.InvalidToken();
                    }
        
                    var user = verifyTokenResult.User;
                    if (!string.IsNullOrEmpty(user.Role))
                    {
                    	var role = await this.roleService.GetByNameAsync(user.Role, user.MembershipId);
                    	if (role != null)
                        {
	                        this.VerifyRolePermissions(role, user.Id, httpContext);
                        }
                    }

                    return user;
				}
				
				case "Basic":
				{
					var validationResult = await this.tokenService.VerifyBasicTokenAsync(token);
					if (!validationResult.IsValidated)
					{
						throw ErtisAuthException.InvalidToken();
					}
					
					var application = validationResult.Application;
					if (!string.IsNullOrEmpty(application.Role))
					{
						var role = await this.roleService.GetByNameAsync(application.Role, application.MembershipId);
						if (role != null)
						{
							this.VerifyRolePermissions(role, application.Id, httpContext);
						}
					}
					
					return application;
				}
				
				default:
					throw ErtisAuthException.UnsupportedTokenType();
			}
		}

		private void VerifyRolePermissions(Role role, string subjectId, HttpContext httpContext)
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

            var rbac = new Rbac(rbacSubjectSegment, rbacResourceSegment, rbacActionSegment, rbacObjectSegment);
            if (!role.HasPermission(rbac))
            {
                throw ErtisAuthException.AccessDenied("Your authorization role is unauthorized for this action");
            }
		}
		
		#endregion
	}
}