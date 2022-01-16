using System.Linq;
using System.Security.Claims;
using ErtisAuth.Core.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ErtisAuth.Hub.Constants;

namespace ErtisAuth.Hub.Extensions
{
    public static class ControllerExtensions
	{
		#region Constants

		private const string RedirectionPassingKey = "redirection_params";

		#endregion
		
		#region Methods

		public static string GetClaim(this ControllerBase controller, string key)
		{
			var claim = controller.User.Claims.FirstOrDefault(x => x.Type == key);
			return claim?.Value;
		}
		
		public static string GetClaim(this ViewComponent viewComponent, string key)
		{
			var claim = viewComponent.HttpContext.User.Claims.FirstOrDefault(x => x.Type == key);
			return claim?.Value;
		}
		
		public static string GetClaim(this HttpContext httpContext, string key)
		{
			var claim = httpContext.User.Claims.FirstOrDefault(x => x.Type == key);
			return claim?.Value;
		}
		
		public static string GetClaim(this AuthorizationHandlerContext authorizationHandlerContext, string key)
		{
			if (authorizationHandlerContext == null)
				return null;
			
			return GetClaim(authorizationHandlerContext.User, key);
		}
		
		public static string GetClaim(this ClaimsPrincipal claimsPrincipal, string key)
		{
			var claim = claimsPrincipal?.Claims.FirstOrDefault(x => x.Type == key);
			return claim?.Value;
		}
		
		public static string GetAccessToken(this ControllerBase controller)
		{
			return GetClaim(controller, Claims.AccessToken);
		}
		
		public static string GetAccessToken(this ViewComponent viewComponent)
		{
			return GetClaim(viewComponent, Claims.AccessToken);
		}
		
		public static string GetAccessToken(this AuthorizationHandlerContext authorizationHandlerContext)
		{
			return GetClaim(authorizationHandlerContext, Claims.AccessToken);
		}
		
		public static string GetAccessToken(this ClaimsPrincipal claimsPrincipal)
		{
			return GetClaim(claimsPrincipal, Claims.AccessToken);
		}
		
		public static BearerToken GetBearerToken(this ControllerBase controller)
		{
			return BearerToken.CreateTemp(controller.GetAccessToken());
		}
		
		public static BearerToken GetBearerToken(this ViewComponent viewComponent)
		{
			return BearerToken.CreateTemp(viewComponent.GetAccessToken());
		}
		
		public static BearerToken GetBearerToken(this AuthorizationHandlerContext authorizationHandlerContext)
		{
			return BearerToken.CreateTemp(authorizationHandlerContext.GetAccessToken());
		}
		
		public static BearerToken GetBearerToken(this ClaimsPrincipal claimsPrincipal)
		{
			return BearerToken.CreateTemp(claimsPrincipal.GetAccessToken());
		}

		public static void SetRedirectionParameter<T>(this Controller controller, T arg)
		{
			if (arg != null)
			{
				controller.TempData[RedirectionPassingKey] = Newtonsoft.Json.JsonConvert.SerializeObject(arg);
			}
		}
		
		public static T GetRedirectionParameter<T>(this Controller controller)
		{
			if (controller.TempData.ContainsKey(RedirectionPassingKey))
			{
				var jObject = controller.TempData[RedirectionPassingKey];
				controller.TempData.Remove(RedirectionPassingKey);
				if (jObject != null)
				{
					var json = jObject.ToString();
					if (!string.IsNullOrEmpty(json))
					{
						return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);	
					}
				}
			}

			return default;
		}
		
		#endregion
	}
}