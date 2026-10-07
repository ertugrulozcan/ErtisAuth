using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// The real ErtisAuth API in memory (TestServer, no TCP port) on its own database of the test MongoDB container.
/// </summary>
public sealed class ErtisAuthFactory : WebApplicationFactory<Program>
{
	#region Fields
	
	private readonly string _connectionString;
	
	private readonly string _databaseName;
	
	private readonly Action<IServiceCollection>? _configureTestServices;
	
	#endregion
	
	#region Constructors
	
	/// <param name="connectionString"></param>
	/// <param name="databaseName"></param>
	/// <param name="configureTestServices">Replaces services of the app (e.g. the HTTP handler of outgoing calls), after its own registrations</param>
	public ErtisAuthFactory(string connectionString, string databaseName, Action<IServiceCollection>? configureTestServices = null)
	{
		this._connectionString = connectionString;
		this._databaseName = databaseName;
		this._configureTestServices = configureTestServices;
	}
	
	#endregion
	
	#region Methods
	
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.UseSetting("Database:ConnectionString", this._connectionString);
		builder.UseSetting("Database:DefaultAuthDatabase", this._databaseName);
		builder.UseSetting("Database:AllowDiskUse", "false");
		
		if (this._configureTestServices != null)
		{
			builder.ConfigureTestServices(this._configureTestServices);
		}
	}
	
	#endregion
}