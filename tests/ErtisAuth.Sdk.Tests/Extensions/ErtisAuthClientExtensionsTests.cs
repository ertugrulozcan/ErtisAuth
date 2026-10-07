using Ertis.Net.Rest;
using ErtisAuth.Sdk.Attributes;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ErtisAuth.Sdk.Tests.Extensions;

/// <summary>
/// Registration of the SDK in any kind of application (AddErtisAuth of ErtisAuth.Sdk).
/// The default reads appsettings.json of the test project (copied to the output directory).
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
	
	#endregion
	
	#region Configuration Sources
	
	[Fact]
	public void AddErtisAuth_ByDefault_ReadsErtisAuthSectionOfAppSettings()
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
