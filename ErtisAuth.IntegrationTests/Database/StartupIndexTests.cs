using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Database;

/// <summary>
/// The indexes created at startup (UseMongoDBAsync). The TTL indexes replace the removed cleanup jobs.
/// The text indexes serve the search endpoints.
/// </summary>
public class StartupIndexTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	private readonly MongoDbContainerFixture _mongo;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	/// <param name="mongo"></param>
	public StartupIndexTests(ErtisAuthInstance instance, MongoDbContainerFixture mongo)
	{
		this._instance = instance;
		this._mongo = mongo;
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
	public async Task Startup_CreatesTheUniqueUserCodeIndex()
	{
		// Two token codes generated at the same time can't get the same user code
		var codes = this._instance.Database.GetCollection<BsonDocument>("codes");
		var indexes = await (await codes.Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);

		var index = Assert.Single(indexes, x => x["key"].AsBsonDocument.Names.SequenceEqual(["membership_id", "user_code"]));
		Assert.True(index.GetValue("unique", false).ToBoolean(), index.ToJson());
	}

	[Fact]
	public async Task Startup_CreatesTheRevokedTokenLookupIndex()
	{
		var indexes = await (await this._instance.Database.GetCollection<BsonDocument>("revoked_tokens").Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		
		Assert.Contains(indexes, x => x["key"].AsBsonDocument.Names.SequenceEqual(["token"]));
	}
	
	[Fact]
	public async Task Startup_CreatesTheRefreshTokenLookupIndex()
	{
		var indexes = await (await this._instance.Database.GetCollection<BsonDocument>("active_tokens").Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		
		Assert.Contains(indexes, x => x["key"].AsBsonDocument.Names.SequenceEqual(["refresh_token"]));
	}
	
	[Theory]
	[InlineData("users", new[] { "username", "firstname", "lastname", "email_address" })]
	[InlineData("roles", new[] { "name", "slug", "description" })]
	[InlineData("applications", new[] { "name", "slug" })]
	[InlineData("memberships", new[] { "name", "slug" })]
	public async Task Startup_CreatesTheTextIndexes(string collection, string[] fields)
	{
		var indexes = await (await this._instance.Database.GetCollection<BsonDocument>(collection).Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		
		var textIndex = Assert.Single(indexes, x => x.Contains("weights"));
		Assert.Equal(fields.Order(), textIndex["weights"].AsBsonDocument.Names.Order());
	}
	
	/// <summary>
	/// Production has a text index on users that was created by hand, under MongoDB's default name. A collection can have
	/// only one text index: the existing one is kept, and the other indexes are still created.
	/// </summary>
	[Fact]
	public async Task Startup_KeepsAnExistingTextIndex()
	{
		var databaseName = $"ertisauth-{Guid.NewGuid():N}";
		var database = new MongoClient(this._mongo.ConnectionString).GetDatabase(databaseName);
		try
		{
			var users = database.GetCollection<BsonDocument>("users");
			var keys = Builders<BsonDocument>.IndexKeys.Text("username").Text("firstname").Text("lastname").Text("email_address");
			var existingName = await users.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys), cancellationToken: TestContext.Current.CancellationToken);
			
			await using (var factory = new ErtisAuthFactory(this._mongo.ConnectionString, databaseName))
			{
				factory.UseKestrel(0);
				using var client = factory.CreateClient();
			}
			
			var indexes = await (await users.Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
			var textIndex = Assert.Single(indexes, x => x.Contains("weights"));
			Assert.Equal(existingName, textIndex["name"].AsString);
			Assert.Equal("english", textIndex["default_language"].AsString);
			Assert.Contains(indexes, x => x["key"].AsBsonDocument.Names.SequenceEqual(["username", "membership_id"]));
		}
		finally
		{
			await database.Client.DropDatabaseAsync(databaseName, TestContext.Current.CancellationToken);
		}
	}
	
	/// <summary>
	/// Indexes are created one by one: one that can't be created (here the name 'membership_id_1' is taken by an index on
	/// another field) doesn't keep the others of the collection from being created.
	/// </summary>
	[Fact]
	public async Task Startup_AnIndexThatCanNotBeCreated_DoesNotBlockTheOthers()
	{
		var databaseName = $"ertisauth-{Guid.NewGuid():N}";
		var database = new MongoClient(this._mongo.ConnectionString).GetDatabase(databaseName);
		try
		{
			var roles = database.GetCollection<BsonDocument>("roles");
			await roles.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("slug"), new CreateIndexOptions { Name = "membership_id_1" }), cancellationToken: TestContext.Current.CancellationToken);
			
			await using (var factory = new ErtisAuthFactory(this._mongo.ConnectionString, databaseName))
			{
				factory.UseKestrel(0);
				using var client = factory.CreateClient();
			}
			
			var indexes = await (await roles.Indexes.ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
			
			// The conflicting index is left as it is
			var conflicting = Assert.Single(indexes, x => x["name"].AsString == "membership_id_1");
			Assert.Equal(["slug"], conflicting["key"].AsBsonDocument.Names);
			
			// The others are created
			var names = indexes.Select(x => x["name"].AsString).ToArray();
			Assert.Contains("name_1", names);
			Assert.Contains("_id_1_membership_id_1", names);
			Assert.Contains("name_1_membership_id_1", names);
			Assert.Contains(indexes, x => x.Contains("weights"));
		}
		finally
		{
			await database.Client.DropDatabaseAsync(databaseName, TestContext.Current.CancellationToken);
		}
	}
	
	#endregion
}