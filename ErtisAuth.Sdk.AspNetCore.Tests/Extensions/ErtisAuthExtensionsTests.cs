using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.AspNetCore.Extensions;
using ErtisAuth.Sdk.AspNetCore.Middleware;
using ErtisAuth.Sdk.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
	
	#endregion
	
	#region Helper Classes
	
	private sealed class CustomAuthenticationHandler(
		IAuthorizationHandler<BasicToken> basicAuthorizationHandler,
		IAuthorizationHandler<BearerToken> bearerAuthorizationHandler,
		Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		System.Text.Encodings.Web.UrlEncoder encoder)
		: ErtisAuthAuthenticationHandler(basicAuthorizationHandler, bearerAuthorizationHandler, options, logger, encoder);
	
	#endregion
}
