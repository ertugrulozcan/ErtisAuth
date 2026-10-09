using Ertis.Net.Rest;
using ErtisAuth.Sdk.Attributes;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Sdk.Tests.Extensions;

/// <summary>
/// Registration of the SDK in any kind of application (AddErtisAuth of ErtisAuth.Sdk).
/// Without a host, the default reads appsettings.json of the test project (copied to the output directory).
/// </summary>
public class ErtisAuthClientExtensionsTests
{
	#region Helpers
	
	private static IServiceProvider BuildManually(Action<ErtisAuthOptions>? configure = null)
	{
		return new ServiceCollection()
			.AddErtisAuth(options =>
			{
				options.BaseUrl = "https://auth.test/api/v1";
				options.MembershipId = "membership-id";
				configure?.Invoke(options);
			})
			.BuildServiceProvider();
	}
	
	private static OptionsValidationException AssertInvalid(Action<ErtisAuthOptions> configure)
	{
		return Assert.Throws<OptionsValidationException>(() => new ServiceCollection().AddErtisAuth(configure));
	}
	
	private static IConfigurationRoot CreateConfiguration(Dictionary<string, string?> values)
	{
		return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
	}
	
	/// <summary>
	/// As the hosts register it (WebApplication.CreateBuilder, Host.CreateApplicationBuilder…): a factory, so the configuration
	/// is not available until the service provider is built
	/// </summary>
	private static ServiceCollection CreateHostServices(Dictionary<string, string?> values)
	{
		var configuration = CreateConfiguration(values);
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(_ => configuration);
		return services;
	}
	
	#endregion
	
	#region Configuration Sources
	
	[Fact]
	public void AddErtisAuth_WithoutHost_ReadsErtisAuthSectionOfAppSettings()
	{
		var options = new ServiceCollection().AddErtisAuth().BuildServiceProvider().GetRequiredService<IErtisAuthOptions>();
		
		Assert.Equal("https://auth.from-file.test/api/v1", options.BaseUrl);
		Assert.Equal("membership-from-default-section", options.MembershipId);
		Assert.Equal(30, options.BasicTokenCacheTTL);
	}
	
	[Fact]
	public void AddErtisAuth_WithSectionName_ReadsThatSection()
	{
		var options = new ServiceCollection().AddErtisAuth("Custom:Auth").BuildServiceProvider().GetRequiredService<IErtisAuthOptions>();
		
		Assert.Equal("https://auth.custom.test/api/v1", options.BaseUrl);
		Assert.Equal("membership-from-custom-section", options.MembershipId);
	}
	
	[Fact]
	public void AddErtisAuth_WithManualOptions_UsesThemOnly()
	{
		var serviceProvider = BuildManually(x => x.BasicTokenCacheTTL = 60);
		
		var options = serviceProvider.GetRequiredService<IErtisAuthOptions>();
		Assert.Equal("https://auth.test/api/v1", options.BaseUrl);
		Assert.Equal("membership-id", options.MembershipId);
		Assert.Equal(60, options.BasicTokenCacheTTL);
		Assert.Same(options, serviceProvider.GetRequiredService<IOptions<ErtisAuthOptions>>().Value);
	}
	
	[Fact]
	public void AddErtisAuth_WithMissingSection_FailsAtRegistration()
	{
		var exception = Assert.Throws<OptionsValidationException>(() => new ServiceCollection().AddErtisAuth("Missing"));
		
		Assert.Contains(exception.Failures, x => x.Contains("configuration section 'Missing'") && x.Contains("BaseUrl is required"));
		Assert.Contains(exception.Failures, x => x.Contains("MembershipId is required"));
	}
	
	[Fact]
	public void AddErtisAuth_WithConfigurationSection_ReadsThatSection()
	{
		var configuration = CreateConfiguration(new Dictionary<string, string?>
		{
			["Identity:BaseUrl"] = "https://auth.from-section.test",
			["Identity:MembershipId"] = "membership-from-section"
		});
		
		var options = new ServiceCollection().AddErtisAuth(configuration.GetSection("Identity")).BuildServiceProvider().GetRequiredService<IErtisAuthOptions>();
		
		Assert.Equal("https://auth.from-section.test", options.BaseUrl);
		Assert.Equal("membership-from-section", options.MembershipId);
	}
	
	[Fact]
	public void AddErtisAuth_WithInvalidConfigurationSection_FailsAtRegistration()
	{
		var configuration = CreateConfiguration(new Dictionary<string, string?> { ["Identity:BaseUrl"] = "not-a-url" });
		
		var exception = Assert.Throws<OptionsValidationException>(() => new ServiceCollection().AddErtisAuth(configuration.GetSection("Identity")));
		
		Assert.Contains(exception.Failures, x => x.Contains("configuration section 'Identity'") && x.Contains("absolute http or https url"));
		Assert.Contains(exception.Failures, x => x.Contains("MembershipId is required"));
	}
	
	#endregion
	
	#region Host Configuration
	
	[Fact]
	public void AddErtisAuth_WithHost_ReadsTheConfigurationOfTheHost()
	{
		// A source of the host the SDK can't know (e.g. user secrets); appsettings.json of the test project has other values
		var services = CreateHostServices(new Dictionary<string, string?>
		{
			["ErtisAuth:BaseUrl"] = "https://auth.from-host.test",
			["ErtisAuth:MembershipId"] = "membership-from-host",
			["ErtisAuth:BasicTokenCacheTTL"] = "15"
		});
		
		var options = services.AddErtisAuth().BuildServiceProvider().GetRequiredService<IErtisAuthOptions>();
		
		Assert.Equal("https://auth.from-host.test", options.BaseUrl);
		Assert.Equal("membership-from-host", options.MembershipId);
		Assert.Equal(15, options.BasicTokenCacheTTL);
	}
	
	[Fact]
	public void AddErtisAuth_WithHostAndSectionName_ReadsThatSectionOfTheHost()
	{
		var services = CreateHostServices(new Dictionary<string, string?>
		{
			["Identity:BaseUrl"] = "https://auth.from-host.test",
			["Identity:MembershipId"] = "membership-from-host"
		});
		
		var options = services.AddErtisAuth("Identity").BuildServiceProvider().GetRequiredService<IErtisAuthOptions>();
		
		Assert.Equal("membership-from-host", options.MembershipId);
	}
	
	[Fact]
	public void AddErtisAuth_WithHostAndInvalidConfiguration_FailsWhenTheHostStarts()
	{
		var services = CreateHostServices(new Dictionary<string, string?> { ["ErtisAuth:BaseUrl"] = "not-a-url" });
		
		// The configuration of the host is only read once the service provider is built
		var serviceProvider = services.AddErtisAuth().BuildServiceProvider();
		
		// What the host runs on start (ValidateOnStart)
		var exception = Assert.Throws<OptionsValidationException>(() => serviceProvider.GetRequiredService<IStartupValidator>().Validate());
		Assert.Contains(exception.Failures, x => x.Contains("configuration section 'ErtisAuth'") && x.Contains("absolute http or https url"));
		Assert.Contains(exception.Failures, x => x.Contains("MembershipId is required"));
	}
	
	[Fact]
	public void AddErtisAuth_WithHostAndInvalidConfiguration_FailsOnFirstUseWithoutHostStart()
	{
		var serviceProvider = CreateHostServices([]).AddErtisAuth().BuildServiceProvider();
		
		Assert.Throws<OptionsValidationException>(() => serviceProvider.GetRequiredService<IErtisAuthOptions>());
	}
	
	#endregion
	
	#region Validation
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void AddErtisAuth_WithoutBaseUrl_Fails(string? baseUrl)
	{
		var exception = AssertInvalid(x => { x.BaseUrl = baseUrl; x.MembershipId = "membership-id"; });
		
		Assert.Contains(exception.Failures, x => x.Contains("BaseUrl is required"));
	}
	
	[Theory]
	[InlineData("auth.test/api/v1")]
	[InlineData("/api/v1")]
	[InlineData("ftp://auth.test")]
	public void AddErtisAuth_WithBaseUrlNotAbsoluteHttp_Fails(string baseUrl)
	{
		var exception = AssertInvalid(x => { x.BaseUrl = baseUrl; x.MembershipId = "membership-id"; });
		
		Assert.Contains(exception.Failures, x => x.Contains("BaseUrl must be an absolute http or https url"));
	}
	
	[Fact]
	public void AddErtisAuth_WithoutMembershipId_Fails()
	{
		var exception = AssertInvalid(x => x.BaseUrl = "https://auth.test");
		
		Assert.Contains(exception.Failures, x => x.Contains("MembershipId is required"));
	}
	
	[Fact]
	public void AddErtisAuth_WithNegativeBasicTokenCacheTTL_Fails()
	{
		var exception = AssertInvalid(x => { x.BaseUrl = "https://auth.test"; x.MembershipId = "membership-id"; x.BasicTokenCacheTTL = -1; });
		
		Assert.Contains(exception.Failures, x => x.Contains("BasicTokenCacheTTL can not be negative"));
	}
	
	[Fact]
	public void AddErtisAuth_WithSeveralInvalidSettings_ReportsAllOfThem()
	{
		var exception = AssertInvalid(x => { x.BaseUrl = "ftp://auth.test"; x.BasicTokenCacheTTL = -1; });
		
		Assert.Equal(3, exception.Failures.Count());
		Assert.All(exception.Failures, x => Assert.Contains("(manual configuration)", x));
	}
	
	[Theory]
	[InlineData("http://auth.test")]
	[InlineData("https://auth.test/api/v1")]
	public void AddErtisAuth_WithValidOptions_Succeeds(string baseUrl)
	{
		var serviceProvider = BuildManually(x => x.BaseUrl = baseUrl);
		
		Assert.Equal(baseUrl, serviceProvider.GetRequiredService<IErtisAuthOptions>().BaseUrl);
	}
	
	#endregion
	
	#region Registrations
	
	[Fact]
	public void AddErtisAuth_RegistersEveryServiceInterface()
	{
		var serviceProvider = BuildManually();
		var serviceInterfaces = typeof(ServiceLifetimeAttribute).Assembly.GetTypes()
			.Where(x => x.IsInterface && x.GetCustomAttributes(typeof(ServiceLifetimeAttribute), true).Length > 0)
			.ToArray();
		
		Assert.NotEmpty(serviceInterfaces);
		Assert.All(serviceInterfaces, x => Assert.NotNull(serviceProvider.GetService(x)));
	}
	
	[Fact]
	public void AddErtisAuth_RegistersRestHandlerWithHttpClientFactory()
	{
		var serviceProvider = BuildManually();
		
		Assert.NotNull(serviceProvider.GetService<IHttpClientFactory>());
		Assert.IsType<RestHandler>(serviceProvider.GetRequiredService<IRestHandler>());
	}
	
	[Fact]
	public void AddErtisAuth_CalledTwice_KeepsFirstRegistration()
	{
		var services = new ServiceCollection();
		services.AddErtisAuth(x => { x.BaseUrl = "https://first.test"; x.MembershipId = "first"; });
		var registrationCount = services.Count;
		
		services.AddErtisAuth(x => { x.BaseUrl = "https://second.test"; x.MembershipId = "second"; });
		
		Assert.Equal(registrationCount, services.Count);
		Assert.Equal("first", services.BuildServiceProvider().GetRequiredService<IErtisAuthOptions>().MembershipId);
	}
	
	#endregion
}
