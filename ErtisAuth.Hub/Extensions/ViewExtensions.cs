using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Hub.Extensions
{
    public static class ViewExtensions
    {
        #region Methods

        public static bool IsAuthorizedAction(this RazorPage page, string resource, string action, string @object)
        {
            var userId = page.Context.GetClaim(Claims.UserId);
            var accessToken = page.Context.GetClaim(Claims.AccessToken);
            
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(accessToken))
            {
                if (!string.IsNullOrEmpty(resource) && !string.IsNullOrEmpty(action))
                {
                    var middlewareRoleService = page.Context.RequestServices.GetService<IMiddlewareRoleService>();
                    if (middlewareRoleService != null)
                    {
                        var subjectSegment = new RbacSegment(userId);
                        var resourceSegment = new RbacSegment(resource);
                        var actionSegment = new RbacSegment(action);
                        var objectSegment = !string.IsNullOrEmpty(@object) ? new RbacSegment(@object) : RbacSegment.All;
                    
                        var rbac = new Rbac(subjectSegment, resourceSegment, actionSegment, objectSegment);
                        return middlewareRoleService.CheckPermission(rbac, BearerToken.CreateTemp(accessToken));   
                    }
                }
            }

            return false;
        }
        
        public static bool IsAuthorizedAction(this RazorPage page, RbacSegment resourceSegment, RbacSegment actionSegment, RbacSegment objectSegment)
        {
            var userId = page.Context.GetClaim(Claims.UserId);
            var accessToken = page.Context.GetClaim(Claims.AccessToken);
            
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(accessToken))
            {
                var middlewareRoleService = page.Context.RequestServices.GetService<IMiddlewareRoleService>();
                if (middlewareRoleService != null)
                {
                    var subjectSegment = new RbacSegment(userId);
                    
                    var rbac = new Rbac(subjectSegment, resourceSegment, actionSegment, objectSegment);
                    return middlewareRoleService.CheckPermission(rbac, BearerToken.CreateTemp(accessToken));   
                }
            }

            return false;
        }
        
        public static bool IsAuthorizedAction(this RazorPage page, string resource, RbacSegment actionSegment, RbacSegment objectSegment)
        {
            var userId = page.Context.GetClaim(Claims.UserId);
            var accessToken = page.Context.GetClaim(Claims.AccessToken);
            
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(accessToken))
            {
                if (!string.IsNullOrEmpty(resource))
                {
                    var middlewareRoleService = page.Context.RequestServices.GetService<IMiddlewareRoleService>();
                    if (middlewareRoleService != null)
                    {
                        var subjectSegment = new RbacSegment(userId);
                        var resourceSegment = new RbacSegment(resource);
                    
                        var rbac = new Rbac(subjectSegment, resourceSegment, actionSegment, objectSegment);
                        return middlewareRoleService.CheckPermission(rbac, BearerToken.CreateTemp(accessToken));   
                    }   
                }
            }

            return false;
        }

        #endregion
    }
}