using Testcontainers.MongoDb;

// Docker is required; pipelines without Docker can exclude these tests by this trait
[assembly: Trait("Category", "Integration")]
[assembly: AssemblyFixture(typeof(ErtisAuth.IntegrationTests.Infrastructure.MongoDbContainerFixture))]

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// One MongoDB container for the whole test run (each test class uses its own database in it).
/// A replica set like production; the data directory is in memory (tmpfs), so nothing is written to the disk.
/// The container gets a random host port, so a local MongoDB on 27017 is not affected.
/// </summary>
public sealed class MongoDbContainerFixture : IAsyncLifetime
{
	#region Constants
	
	/// <summary>
	/// The production version line (7.0.43). Override with ERTISAUTH_TEST_MONGO_IMAGE. Note: MongoDB 8.0 refuses to start
	/// on Linux kernels 6.19+ (SERVER-121912), e.g. recent Docker Desktop kernels; 7.0 and 8.2 start there.
	/// </summary>
	private const string DefaultImage = "mongo:7.0";
	
	#endregion
	
	#region Fields
	
	private readonly MongoDbContainer _container = new MongoDbBuilder(Environment.GetEnvironmentVariable("ERTISAUTH_TEST_MONGO_IMAGE") ?? DefaultImage)
		.WithReplicaSet()
		.WithTmpfsMount("/data/db")
		.Build();
	
	#endregion
	
	#region Properties
	
	public string ConnectionString => this._container.GetConnectionString();
	
	#endregion
	
	#region Methods
	
	public async ValueTask InitializeAsync()
	{
		await this._container.StartAsync();
	}
	
	public async ValueTask DisposeAsync()
	{
		await this._container.DisposeAsync();
	}
	
	#endregion
}