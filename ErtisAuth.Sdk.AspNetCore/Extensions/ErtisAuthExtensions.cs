using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Extensions;
using ErtisAuth.Sdk.AspNetCore.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Sdk.AspNetCore.Extensions;

/// <summary>
/// Registers the ErtisAuth client services (ErtisAuthClientExtensions.AddErtisAuth of ErtisAuth.Sdk) and the ASP.NET Core authentication and authorization on top.
/// </summary>
// ReSharper disable once UnusedType.Global
public static class ErtisAuthExtensions
{
	#region Methods
	
	/// <summary>
	/// Reads the options from appsettings.json (and appsettings.{ASPNETCORE_ENVIRONMENT}.json) and environment variables.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="sectionName">The configuration section of the options</param>
	public static IServiceCollection AddErtisAuth(this IServiceCollection services, string sectionName = ErtisAuthClientExtensions.DefaultSectionName)
	{
		return services.AddErtisAuth<ErtisAuthAuthenticationHandler>(sectionName);
	}
	
	/// <summary>
	/// Uses the given options only; no configuration file is read.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configure"></param>
	// ReSharper disable once UnusedMethodReturnValue.Global
	public static IServiceCollection AddErtisAuth(this IServiceCollection services, Action<ErtisAuthOptions> configure)
	{
		return services.AddErtisAuth<ErtisAuthAuthenticationHandler>(configure);
	}
	
	// ReSharper disable once MemberCanBePrivate.Global
	public static IServiceCollection AddErtisAuth<TAuthenticationHandler>(this IServiceCollection services, string sectionName = ErtisAuthClientExtensions.DefaultSectionName)
		where TAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		// Called through the class name: the client registration of ErtisAuth.Sdk has the same name
		ErtisAuthClientExtensions.AddErtisAuth(services, sectionName);
		return services.AddErtisAuthAspNetCore<TAuthenticationHandler>();
	}
	
	// ReSharper disable once MemberCanBePrivate.Global
	public static IServiceCollection AddErtisAuth<TAuthenticationHandler>(this IServiceCollection services, Action<ErtisAuthOptions> configure)
		where TAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		// Called through the class name: the client registration of ErtisAuth.Sdk has the same name
		ErtisAuthClientExtensions.AddErtisAuth(services, configure);
		return services.AddErtisAuthAspNetCore<TAuthenticationHandler>();
	}
	
	private static IServiceCollection AddErtisAuthAspNetCore<TAuthenticationHandler>(this IServiceCollection services)
		where TAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		// The first registration wins: adding the authentication scheme again would fail ("Scheme already exists")
		if (services.Any(x => x.ServiceType == typeof(IAuthorizationHandler<BasicToken>)))
		{
			return services;
		}
		
		services.TryAddSingleton<IAuthorizationHandler<BasicToken>, BasicAuthorizationHandler>();
		services.TryAddSingleton<IAuthorizationHandler<BearerToken>, BearerAuthorizationHandler>();
		
		// Authentication
		services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TAuthenticationHandler>(ErtisAuth.Extensions.Authorization.Scheme.Name, _ => { });
		
		// Authorization
		services.AddAuthorization(authorizationOptions =>
			authorizationOptions.AddPolicy(ErtisAuth.Extensions.Authorization.Policy.Name, policy =>
			{
				policy.AddAuthenticationSchemes(ErtisAuth.Extensions.Authorization.Scheme.Name);
				policy.AddRequirements(new ErtisAuthAuthorizationRequirement());
			}));
		
		services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ErtisAuthAuthorizationHandler>());
		
		// Memory Cache
		services.AddMemoryCache();
		
		return services;
	}
	
	#endregion
}
