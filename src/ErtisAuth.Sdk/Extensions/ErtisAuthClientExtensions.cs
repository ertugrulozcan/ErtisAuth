using Ertis.Net.Rest;
using ErtisAuth.Sdk.Attributes;
using ErtisAuth.Sdk.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global
namespace ErtisAuth.Sdk.Extensions;

/// <summary>
/// Registers the ErtisAuth client services, for any kind of application. ASP.NET Core applications use AddErtisAuth
/// of ErtisAuth.Sdk.AspNetCore instead (same name, namespace ErtisAuth.Sdk.AspNetCore.Extensions), which also calls this.
/// The first registration wins; calling it again is a no-op.
/// </summary>
public static class ErtisAuthClientExtensions
{
	#region Constants
	
	public const string DefaultSectionName = "ErtisAuth";
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Reads the options from appsettings.json (and appsettings.{ASPNETCORE_ENVIRONMENT}.json) and environment variables.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="sectionName">The configuration section of the options</param>
	public static IServiceCollection AddErtisAuth(this IServiceCollection services, string sectionName = DefaultSectionName)
	{
		return services.AddErtisAuth(ReadOptions(sectionName), $"configuration section '{sectionName}'");
	}
	
	/// <summary>
	/// Uses the given options only; no configuration file is read.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configure"></param>
	public static IServiceCollection AddErtisAuth(this IServiceCollection services, Action<ErtisAuthOptions> configure)
	{
		var options = new ErtisAuthOptions();
		configure(options);
		return services.AddErtisAuth(options, "manual configuration");
	}
	
	private static IServiceCollection AddErtisAuth(this IServiceCollection services, ErtisAuthOptions options, string source)
	{
		if (services.Any(x => x.ServiceType == typeof(IErtisAuthOptions)))
		{
			return services;
		}
		
		ErtisAuthOptionsValidator.Validate(options, source);
		
		services.Configure<ErtisAuthOptions>(x =>
		{
			x.BaseUrl = options.BaseUrl;
			x.MembershipId = options.MembershipId;
			x.BasicTokenCacheTTL = options.BasicTokenCacheTTL;
		});
		
		services.TryAddSingleton<IErtisAuthOptions>(sp => sp.GetRequiredService<IOptions<ErtisAuthOptions>>().Value);
		
		// RestHandler requires IHttpClientFactory, which is not registered by default (not even in ASP.NET Core)
		services.AddHttpClient();
		services.TryAddSingleton<IRestHandler, RestHandler>();
		
		// SDK services
		InitializeServices(services);
		
		return services;
	}
	
	/// <summary>
	/// Reads the options section from appsettings.json and environment variables (the default behavior of the SDK).
	/// </summary>
	public static ErtisAuthOptions ReadOptions(string sectionName = DefaultSectionName)
	{
		var options = new ErtisAuthOptions();
		BuildConfiguration().GetSection(sectionName).Bind(options);
		return options;
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
	
	private static void InitializeServices(IServiceCollection services)
	{
		var assembly = typeof(ServiceLifetimeAttribute).Assembly;
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
				services.TryAdd(new ServiceDescriptor(baseInterface, implementation, serviceLifetimeAttribute.Lifetime));
				Console.WriteLine($"{baseInterface.Name} resolved as {implementation.Name}");
			}
		}
	}
	
	#endregion
}
