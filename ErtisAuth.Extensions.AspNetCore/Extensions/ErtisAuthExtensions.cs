using System;
using Ertis.Net.Rest;
using ErtisAuth.Extensions.Authorization.Constants;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AuthenticationService = ErtisAuth.Sdk.Services.AuthenticationService;
using IAuthenticationService = ErtisAuth.Sdk.Services.Interfaces.IAuthenticationService;

namespace ErtisAuth.Extensions.AspNetCore.Extensions
{
	public static class ErtisAuthExtensions
	{
		#region Methods

		public static void AddErtisAuth(this IServiceCollection services)
		{
			const string schemeName = Constants.Schemes.ErtisAuthAuthorizationSchemeName;
			const string policyName = Policies.ErtisAuthAuthorizationPolicyName;

			var configuration = BuildConfiguration();
			services.Configure<ErtisAuthOptions>(configuration.GetSection("ErtisAuth"));
			services.AddSingleton<IErtisAuthOptions>(sp => sp.GetRequiredService<IOptions<ErtisAuthOptions>>().Value);

			// RestHandler registration
			services.AddSingleton<IRestHandler, SystemRestHandler>();
			
			// Service registrations
			services.AddSingleton<IAuthenticationService, AuthenticationService>();
			services.AddSingleton<IApplicationService, ApplicationService>();
			services.AddSingleton<IRoleService, RoleService>();
			services.AddSingleton<IPasswordService, PasswordService>();
			
			// Authentication
			services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ErtisAuthAuthenticationHandler>(schemeName, options =>
			{
				
			});
			
			// Authorization
			services.AddAuthorization(options =>
				options.AddPolicy(policyName, policy =>
				{
					policy.AddAuthenticationSchemes(policyName);
					policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
				}));
			
			services.AddSingleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>();
		}
		
		private static IConfigurationRoot BuildConfiguration()
		{
			var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
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

		#endregion
	}
}