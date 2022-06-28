using System;
using System.Linq;
using Ertis.Net.Rest;
using ErtisAuth.Sdk.Attributes;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.AspNetCore.Extensions
{
	public static class ErtisAuthHubExtensions
	{
		#region Methods

		public static void AddErtisAuthHub(this IServiceCollection services)
		{
			// ErtisAuthOptions
			services.AddScoped<IErtisAuthOptions, ScopedErtisAuthOptions>();
			
			// RestHandler registration
			services.AddScoped<IRestHandler, SystemRestHandler>();

			// Service registrations
			InitializeServices(services);
			
			// Authentication
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
					options =>
					{
						options.LoginPath = new PathString("/login");
						options.LogoutPath = new PathString("/logout");
						options.AccessDeniedPath = new PathString("/unauthorized");
						options.ReturnUrlParameter = "return";
					});

			// Authorization
			services.AddAuthorization(options =>
			{
				options.AddPolicy(
					Constants.Schemes.ErtisAuthAuthorizationSchemeName,
					policy => policy.AddRequirements(new ErtisAuthAuthorizationRequirement()));
			});
		}
		
		private static void InitializeServices(IServiceCollection services)
		{
			var assembly = System.Reflection.Assembly.GetAssembly(typeof(MembershipBoundedService));
			if (assembly == null)
			{
				throw new TypeUnloadedException("ErtisAuth.Sdk assembly was not loaded!");
			}
			
			var types = assembly.GetTypes();
			var interfaces = types
				.Where(x => !string.IsNullOrEmpty(x.FullName) && x.IsInterface && x.FullName.StartsWith("ErtisAuth.Sdk.Services.Interfaces"))
				.Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(ServiceLifetimeAttribute))).ToList();
			
			var implementations = types.Where(x => !string.IsNullOrEmpty(x.FullName) && x.IsClass && x.FullName.StartsWith("ErtisAuth.Sdk.Services"));
			foreach (var implementation in implementations)
			{
				var baseInterface = interfaces.FirstOrDefault(x => x.IsAssignableFrom(implementation));
				if (baseInterface != null)
				{
					services.AddScoped(baseInterface, implementation);
					Console.WriteLine($"{baseInterface.Name} resolved as {implementation.Name}");	
				}
			}
		}

		#endregion
	}
}