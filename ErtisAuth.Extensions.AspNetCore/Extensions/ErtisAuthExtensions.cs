using ErtisAuth.Extensions.Authorization.Constants;
using ErtisAuth.Sdk.Services;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
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

		#endregion
	}
}