using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ertis.Extensions.AspNetCore.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/api-map")]
	public class ApiMapController : ControllerBase
	{
		#region Services

		private readonly IApiVersionOptions apiVersion;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="apiVersionOptions"></param>
		public ApiMapController(IApiVersionOptions apiVersionOptions)
		{
			this.apiVersion = apiVersionOptions;
		}

		#endregion
		
		#region Methods

		[HttpGet]
		public IActionResult Get()
		{
			var apiMapDictionary = new Dictionary<string, List<string>>();
			
			var type = this.GetType();
			var controllerNamespace = type.Namespace;
			var controllerClasses = type.Assembly.GetTypes().Where(x => x.IsPublic && x.IsClass && x.Namespace == controllerNamespace).ToList();
			foreach (var controllerClass in controllerClasses)
			{
				var controllerRoute = "";
				var controllerRouteAttribute = controllerClass.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(RouteAttribute));
				if (controllerRouteAttribute != null)
				{
					controllerRoute = controllerRouteAttribute.ConstructorArguments.FirstOrDefault(x => x.ArgumentType == typeof(string)).Value?.ToString();
				}

				controllerRoute = controllerRoute?.Replace("{v:apiVersion}", this.apiVersion.ToString());

				if (controllerRoute != null && controllerRoute.Contains("[controller]") && controllerClass.Name.EndsWith("Controller"))
				{
					var endpointSlug = controllerClass.Name.Replace("Controller", string.Empty).ToLower();
					controllerRoute = controllerRoute.Replace("[controller]", endpointSlug);
				}
				
				controllerRoute = $"/{controllerRoute}";
				
				var methods = controllerClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(x => x.IsPublic);
				foreach (var methodInfo in methods)
				{
					var httpMethodAttribute = methodInfo.CustomAttributes.FirstOrDefault(x => x.AttributeType.BaseType == typeof(HttpMethodAttribute));
					if (httpMethodAttribute != null)
					{
						string httpMethod = httpMethodAttribute.AttributeType.Name.Replace("Http", string.Empty).Replace("Attribute", string.Empty).ToUpper();
						string methodRoute;
						var routeAttribute = methodInfo.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(RouteAttribute));
						if (routeAttribute != null)
						{
							methodRoute = routeAttribute.ConstructorArguments.FirstOrDefault(x => x.ArgumentType == typeof(string)).Value?.ToString();
						}
						else
						{
							methodRoute = httpMethodAttribute.ConstructorArguments.FirstOrDefault(x => x.ArgumentType == typeof(string)).Value?.ToString();
						}

						string route = string.IsNullOrEmpty(methodRoute) ? controllerRoute : $"{controllerRoute}/{methodRoute}";
						if (!string.IsNullOrEmpty(route))
						{
							if (!apiMapDictionary.ContainsKey(route))
								apiMapDictionary.Add(route, new List<string>());
						
							apiMapDictionary[route].Add(httpMethod);	
						}
					}
				}
			}

			if (this.Request.Query.ContainsKey("nested") && this.Request.Query["nested"].ToString().ToLower() == "true")
			{
				return this.Ok(apiMapDictionary.Select(x => new
				{
					url = x.Key,
					methods = x.Value
				}));	
			}
			else
			{
				var stringList = apiMapDictionary.Select(x => $"{x.Key}#[{string.Join(", ", x.Value)}]").ToList();
				int maxLength = stringList.Max(x => x.Length);

				return this.Ok(stringList.Select(x => x.Replace("#", GenerateBlankString(maxLength - x.Length + 10))));
			}
		}

		private static string GenerateBlankString(int length)
		{
			string blank = string.Empty;
			for (int i = 0; i < length; i++)
			{
				blank += " ";
			}

			return blank;
		}
		
		#endregion
	}
}