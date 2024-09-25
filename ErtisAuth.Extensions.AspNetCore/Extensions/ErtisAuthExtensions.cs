using System;
using System.Linq;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Configuration;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Sdk.Attributes;
using ErtisAuth.Sdk.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Extensions.AspNetCore.Extensions
{
	public static class ErtisAuthExtensions
	{
		#region Methods

		// ReSharper disable once MemberCanBePrivate.Global
		public static void AddErtisAuth(this IServiceCollection services, Action<ErtisAuthBootloaderOptions> ertisAuthBootloaderOptions = null)
		{
			AddErtisAuth<ErtisAuthAuthenticationHandler>(services, ertisAuthBootloaderOptions);
		}
			
		// ReSharper disable once MemberCanBePrivate.Global
		public static void AddErtisAuth<TAuthenticationHandler>(this IServiceCollection services, Action<ErtisAuthBootloaderOptions> ertisAuthBootloaderOptions = null) where TAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
		{
			var options = new ErtisAuthBootloaderOptions();
			ertisAuthBootloaderOptions?.Invoke(options);

			var configuration = BuildConfiguration(options.Environment);
			services.Configure<ErtisAuthOptions>(configuration.GetSection(options.ConfigurationSectionName));
			services.AddSingleton<IErtisAuthOptions>(sp => sp.GetRequiredService<IOptions<ErtisAuthOptions>>().Value);
			
			// BasicAuthorizationHandler
			if (options.BasicAuthorizationHandlerType != null)
			{
				services.AddSingleton(typeof(IAuthorizationHandler<BasicToken>), options.BasicAuthorizationHandlerType);
			}
			else
			{
				services.AddSingleton<IAuthorizationHandler<BasicToken>, BasicAuthorizationHandler>();
			}
			
			// BearerAuthorizationHandler
			if (options.BearerAuthorizationHandlerType != null)
			{
				services.AddSingleton(typeof(IAuthorizationHandler<BearerToken>), options.BearerAuthorizationHandlerType);
			}
			else
			{
				services.AddSingleton<IAuthorizationHandler<BearerToken>, BearerAuthorizationHandler>();
			}
			
			// RestHandler registration
			services.AddSingleton(typeof(IRestHandler), options.RestHandlerType);

			// Service registrations
			InitializeServices(services);
			
			// Authentication
			var schemeName = options.AuthorizationSchemeName;
			services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TAuthenticationHandler>(schemeName, _ => { });

			// Authorization
			var policyName = options.AuthorizationPolicyName;
			services.AddAuthorization(authorizationOptions =>
				authorizationOptions.AddPolicy(policyName, policy =>
				{
					policy.AddAuthenticationSchemes(policyName);
					policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
				}));
			
			services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
			
			// Memory Cache
			services.AddMemoryCache();
		}
		
		private static IConfigurationRoot BuildConfiguration(string environment = null)
		{
			var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			if (!string.IsNullOrEmpty(environment))
			{
				environmentName = environment;
			}
			
			if (string.IsNullOrEmpty(environmentName))
			{
				var builder = new ConfigurationBuilder()
					.AddJsonFile("appsettings.json")
					.AddEnvironmentVariables();
				return builder.Build();
			}
			else
			{
				var builder = new ConfigurationBuilder()
					.AddJsonFile("appsettings.json")
					.AddJsonFile($"appsettings.{environmentName}.json", true)
					.AddEnvironmentVariables();
				return builder.Build();
			}
		}

		private static void InitializeServices(IServiceCollection services)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "ErtisAuth.Sdk");
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
				if (baseInterface?.GetCustomAttributes(typeof(ServiceLifetimeAttribute), true).FirstOrDefault() is ServiceLifetimeAttribute serviceLifetimeAttribute)
				{
					// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
					switch (serviceLifetimeAttribute.Lifetime)
					{
						case ServiceLifetime.Singleton:
							services.AddSingleton(baseInterface, implementation);
							break;
						case ServiceLifetime.Scoped:
							services.AddScoped(baseInterface, implementation);
							break;
						case ServiceLifetime.Transient:
							services.AddTransient(baseInterface, implementation);
							break;
					}

					Console.WriteLine($"{baseInterface.Name} resolved as {implementation.Name}");
				}
			}
		}

		#endregion
	}
}