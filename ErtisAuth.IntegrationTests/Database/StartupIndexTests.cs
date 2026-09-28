using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Database;

/// <summary>
/// The indexes created at startup (UseMongoDBAsync). The TTL indexes replace the removed cleanup jobs.
/// </summary>
public class StartupIndexTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public StartupIndexTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[InlineData("active_tokens", "retain_until")]
	[InlineData("revoked_tokens", "retain_until")]
	[InlineData("otps", "token.expire_time")]
	[InlineData("codes", "expire_time")]
	public async Task Startup_CreatesTheTtlIndexes(string collection, string field)
	{
		var indexes = await (await this._instance.Database.GetCollection<BsonDocument>(collection).Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		
		var ttlIndex = Assert.Single(indexes, x => x["key"].AsBsonDocument.Contains(field));
		Assert.True(ttlIndex.Contains("expireAfterSeconds"), ttlIndex.ToJson());
		Assert.True(ttlIndex["expireAfterSeconds"].ToDouble() > 0);
	}
	
	[Fact]
	public async Task Startup_CreatesTheRevokedTokenLookupIndex()
	{
		var indexes = await (await this._instance.Database.GetCollection<BsonDocument>("revoked_tokens").Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		
		Assert.Contains(indexes, x => x["key"].AsBsonDocument.Names.SequenceEqual(["token"]));
	}
	
	#endregion
}