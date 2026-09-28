using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// The real ErtisAuth API in memory (TestServer, no TCP port) on its own database of the test MongoDB container.
/// </summary>
public sealed class ErtisAuthFactory : WebApplicationFactory<Program>
{
	#region Fields
	
	private readonly string _connectionString;
	
	private readonly string _databaseName;
	
	#endregion
	
	#region Constructors
	
	public ErtisAuthFactory(string connectionString, string databaseName)
	{
		this._connectionString = connectionString;
		this._databaseName = databaseName;
	}
	
	#endregion
	
	#region Methods
	
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.UseSetting("Database:ConnectionString", this._connectionString);
		builder.UseSetting("Database:DefaultAuthDatabase", this._databaseName);
		builder.UseSetting("Database:AllowDiskUse", "false");
	}
	
	#endregion
}