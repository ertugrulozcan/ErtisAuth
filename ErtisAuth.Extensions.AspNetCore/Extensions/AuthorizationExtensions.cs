using ErtisAuth.Extensions.AspNetCore.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Extensions.AspNetCore.Extensions;

public static class AuthorizationExtensions
{
	#region Methods
	
	public static void AddErtisAuth(this IServiceCollection services)
	{
		// Authentication
		services
			.AddAuthentication()
			.AddScheme<AuthenticationSchemeOptions, ErtisAuthAuthenticationHandler>(Authorization.Scheme.Name, _ => {});
		
		// Authorization
		services.AddAuthorization(options =>
			options.AddPolicy(Authorization.Policy.Name, policy =>
			{
				policy.AddAuthenticationSchemes(Authorization.Scheme.Name);
				policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
			}));
	}
	
	#endregion
}