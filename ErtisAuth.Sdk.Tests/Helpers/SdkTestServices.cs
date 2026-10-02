using ErtisAuth.Sdk.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Tests.Helpers;

/// <summary>
/// The SDK registered as a client application would (AddErtisAuth), with every HttpClient sending to a recording handler.
/// </summary>
internal sealed class SdkTestServices
{
	#region Constants
	
	public const string BaseUrl = "https://auth.test/api/v1";
	
	public const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	
	#endregion
	
	#region Properties
	
	public RecordingHttpMessageHandler Handler { get; } = new();
	
	// ReSharper disable once MemberCanBePrivate.Global
	public IServiceProvider ServiceProvider { get; }
	
	#endregion
	
	#region Constructors
	
	public SdkTestServices()
	{
		var services = new ServiceCollection();
		services.AddErtisAuth(options =>
		{
			options.BaseUrl = BaseUrl;
			options.MembershipId = MembershipId;
		});
		
		services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => this.Handler));
		this.ServiceProvider = services.BuildServiceProvider();
	}
	
	#endregion
	
	#region Methods
	
	public T Get<T>() where T : notnull => this.ServiceProvider.GetRequiredService<T>();
	
	#endregion
}
