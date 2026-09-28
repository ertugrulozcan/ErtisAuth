using Ertis.Net.Rest;
using ErtisAuth.Integrations.OAuth.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Integrations.OAuth.Tests.Helpers;

/// <summary>
/// The authenticators as the WebAPI registers them (AddProviders + ISystemRestHandler), with every HTTP call answered by <see cref="Handler"/>.
/// </summary>
internal sealed class OAuthTestServices
{
	#region Properties
	
	public RoutingHttpMessageHandler Handler { get; } = new();
	
	private IServiceProvider ServiceProvider { get; }
	
	#endregion
	
	#region Constructors
	
	public OAuthTestServices()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddHttpClient();
		services.AddSingleton<ISystemRestHandler, SystemRestHandler>();
		services.AddProviders();
		services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => this.Handler));
		this.ServiceProvider = services.BuildServiceProvider();
	}
	
	#endregion
	
	#region Methods
	
	public T Get<T>() where T : notnull => this.ServiceProvider.GetRequiredService<T>();
	
	#endregion
}