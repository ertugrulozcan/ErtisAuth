using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.AspNetCore.Extensions;
using ErtisAuth.Sdk.AspNetCore.Middleware;
using ErtisAuth.Sdk.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Sdk.AspNetCore.Tests.Extensions;

/// <summary>
/// Registration of the SDK in ASP.NET Core applications (AddErtisAuth of ErtisAuth.Sdk.AspNetCore).
/// </summary>
public class ErtisAuthExtensionsTests
{
	#region Helpers
	
	private static void Configure(ErtisAuthOptions options)
	{
		options.BaseUrl = "https://auth.test/api/v1";
		options.MembershipId = "membership-id";
	}
	
	private static IServiceProvider Build(Action<IServiceCollection> register)
	{
		var services = new ServiceCollection();
		services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
		services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
		register(services);
		return services.BuildServiceProvider();
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task AddErtisAuth_RegistersErtisAuthSchemeWithHandler()
	{
		var serviceProvider = Build(x => x.AddErtisAuth(Configure));
		
		var scheme = await serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(ErtisAuth.Extensions.Authorization.Scheme.Name);
		
		Assert.NotNull(scheme);
		Assert.Equal(typeof(ErtisAuthAuthenticationHandler), scheme.HandlerType);
	}
	
	[Fact]
	public async Task AddErtisAuth_WithCustomHandler_RegistersIt()
	{
		var serviceProvider = Build(x => x.AddErtisAuth<CustomAuthenticationHandler>(Configure));
		
		var scheme = await serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(ErtisAuth.Extensions.Authorization.Scheme.Name);
		
		Assert.Equal(typeof(CustomAuthenticationHandler), scheme?.HandlerType);
	}
	
	[Fact]
	public async Task AddErtisAuth_RegistersPolicyRequiringErtisAuthScheme()
	{
		var serviceProvider = Build(x => x.AddErtisAuth(Configure));
		
		var policy = await serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(ErtisAuth.Extensions.Authorization.Policy.Name);
		
		Assert.NotNull(policy);
		Assert.Contains(ErtisAuth.Extensions.Authorization.Scheme.Name, policy.AuthenticationSchemes);
		Assert.Contains(policy.Requirements, x => x is ErtisAuthAuthorizationRequirement);
		Assert.Contains(serviceProvider.GetServices<IAuthorizationHandler>(), x => x is ErtisAuthAuthorizationHandler);
	}
	
	[Fact]
	public void AddErtisAuth_RegistersClientServicesAndAuthorizationHandlers()
	{
		var serviceProvider = Build(x => x.AddErtisAuth(Configure));
		
		Assert.Equal("membership-id", serviceProvider.GetRequiredService<IErtisAuthOptions>().MembershipId);
		Assert.NotNull(serviceProvider.GetService<ErtisAuth.Sdk.Services.Interfaces.IAuthenticationService>());
		Assert.NotNull(serviceProvider.GetService<IAuthorizationHandler<BasicToken>>());
		Assert.NotNull(serviceProvider.GetService<IAuthorizationHandler<BearerToken>>());
	}
	
	[Fact]
	public async Task AddErtisAuth_CalledTwice_KeepsSingleSchemeAndHandler()
	{
		var serviceProvider = Build(x =>
		{
			x.AddErtisAuth(Configure);
			x.AddErtisAuth(Configure);
		});
		
		var schemes = await serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();
		
		Assert.Single(schemes, x => x.Name == ErtisAuth.Extensions.Authorization.Scheme.Name);
		Assert.Single(serviceProvider.GetServices<IAuthorizationHandler>(), x => x is ErtisAuthAuthorizationHandler);
	}
	
	/// <summary>
	/// The options come from builder.Configuration, so every source added to it is used (here one that is not a file)
	/// </summary>
	[Fact]
	public void AddErtisAuth_InWebApplication_ReadsBuilderConfiguration()
	{
		var builder = WebApplication.CreateBuilder();
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ErtisAuth:BaseUrl"] = "https://auth.from-builder.test",
			["ErtisAuth:MembershipId"] = "membership-from-builder"
		});
		
		builder.Services.AddErtisAuth();
		using var application = builder.Build();
		
		var options = application.Services.GetRequiredService<IErtisAuthOptions>();
		Assert.Equal("https://auth.from-builder.test", options.BaseUrl);
		Assert.Equal("membership-from-builder", options.MembershipId);
	}
	
	[Fact]
	public async Task AddErtisAuth_InWebApplicationWithInvalidConfiguration_FailsOnStart()
	{
		var builder = WebApplication.CreateBuilder();
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ErtisAuth:BaseUrl"] = "https://auth.from-builder.test" });
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		
		builder.Services.AddErtisAuth();
		await using var application = builder.Build();
		
		var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => application.StartAsync(TestContext.Current.CancellationToken));
		Assert.Contains(exception.Failures, x => x.Contains("configuration section 'ErtisAuth'") && x.Contains("MembershipId is required"));
	}
	
	[Fact]
	public void AddErtisAuth_WithConfigurationSection_RegistersTheSchemeAndReadsThatSection()
	{
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Identity:BaseUrl"] = "https://auth.from-section.test",
			["Identity:MembershipId"] = "membership-from-section"
		}).Build();
		
		var serviceProvider = Build(x => x.AddErtisAuth(configuration.GetSection("Identity")));
		
		Assert.Equal("membership-from-section", serviceProvider.GetRequiredService<IErtisAuthOptions>().MembershipId);
		Assert.Single(serviceProvider.GetServices<IAuthorizationHandler>(), x => x is ErtisAuthAuthorizationHandler);
	}
	
	#endregion
	
	#region Helper Classes
	
	private sealed class CustomAuthenticationHandler(
		IAuthorizationHandler<BasicToken> basicAuthorizationHandler,
		IAuthorizationHandler<BearerToken> bearerAuthorizationHandler,
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		System.Text.Encodings.Web.UrlEncoder encoder)
		: ErtisAuthAuthenticationHandler(basicAuthorizationHandler, bearerAuthorizationHandler, options, logger, encoder);
	
	#endregion
}
