using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// The unique fields of the user types are guarded by unique indexes of the users collection, kept in sync with the user types:
/// email_address and username are unique per membership, any other unique field per membership among the users of the
/// type declaring it and its descendants. A field can not become unique while the users already have duplicate values.
/// </summary>
public class UniqueIndexTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	private readonly MongoDbContainerFixture _mongo;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private IMongoCollection<BsonDocument> Users => this._instance.Database.GetCollection<BsonDocument>("users");
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public UniqueIndexTests(ErtisAuthInstance instance, MongoDbContainerFixture mongo)
	{
		this._instance = instance;
		this._mongo = mongo;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> UserTypesAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/user-types");
	
	private static JsonObject UserTypeBody(string name, string field, bool isUnique, string baseType = "user") => new()
	{
		["name"] = name,
		["baseType"] = baseType,
		["properties"] = new JsonObject
		{
			[field] = new JsonObject { ["type"] = "string", ["isUnique"] = isUnique }
		}
	};
	
	private async Task<JsonObject> CreateUserTypeAsync(string field, bool isUnique = true, string baseType = "user")
	{
		var userTypes = await this.UserTypesAsync();
		return await userTypes.CreateAsync(UserTypeBody(UniqueName("Unique"), field, isUnique, baseType));
	}
	
	private async Task<HttpResponseMessage> CreateUserAsync(string userType, string field, string value)
	{
		var username = $"user{Guid.NewGuid():N}";
		var body = new JsonObject
		{
			["username"] = username,
			["firstname"] = "Unique",
			["lastname"] = "User",
			["email_address"] = $"{username}@example.com",
			["password"] = "Unique-P@ssw0rd!",
			["role"] = "admin",
			["user_type"] = userType,
			[field] = value
		};
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
	}
	
	private async Task<BsonDocument[]> GetUniqueIndexesAsync(string field)
	{
		var indexes = await (await this.Users.Indexes.ListAsync(CancellationToken)).ToListAsync(CancellationToken);
		return indexes.Where(x => x["name"].AsString.StartsWith("ux_") && x["key"].AsBsonDocument.Contains(field)).ToArray();
	}
	
	private static string[] CoveredUserTypes(BsonDocument index) =>
		index["partialFilterExpression"]["user_type"]["$in"].AsBsonArray.Select(x => x.AsString).Order().ToArray();
	
	private static async Task AssertUniqueConstraintErrorAsync(HttpResponseMessage response, string field)
	{
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		var json = error!.ToJsonString(new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		Assert.Contains($"The '{field}' field has unique constraint", json);
	}
	
	private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}";
	
	private static string UniqueField() => $"code_{Guid.NewGuid():N}"[..13];
	
	#endregion
	
	#region Email Address & Username
	
	[Theory]
	[InlineData("email_address")]
	[InlineData("username")]
	public async Task Startup_CreatesTheMembershipWideIndexes(string field)
	{
		var index = Assert.Single(await this.GetUniqueIndexesAsync(field));
		
		Assert.Equal($"ux_{field}", index["name"].AsString);
		Assert.True(index["unique"].AsBoolean);
		Assert.Equal(["membership_id", field], index["key"].AsBsonDocument.Names);
		Assert.Equal(new BsonDocument(field, new BsonDocument { { "$type", "string" }, { "$gt", "" } }), index["partialFilterExpression"].AsBsonDocument);
	}
	
	/// <summary>
	/// The same email address can be used in another membership, but only once in a membership.
	/// </summary>
	[Fact]
	public async Task EmailAddress_IsUniquePerMembership()
	{
		var emailAddress = $"{Guid.NewGuid():N}@example.com";
		var otherMembershipId = ObjectId.GenerateNewId().ToString();
		
		await this.Users.InsertOneAsync(new BsonDocument { { "membership_id", this._instance.MembershipId }, { "email_address", emailAddress } }, cancellationToken: CancellationToken);
		await this.Users.InsertOneAsync(new BsonDocument { { "membership_id", otherMembershipId }, { "email_address", emailAddress } }, cancellationToken: CancellationToken);
		
		var duplicate = new BsonDocument { { "membership_id", this._instance.MembershipId }, { "email_address", emailAddress } };
		var exception = await Assert.ThrowsAsync<MongoWriteException>(() => this.Users.InsertOneAsync(duplicate, cancellationToken: CancellationToken));
		Assert.Equal(ServerErrorCategory.DuplicateKey, exception.WriteError.Category);
		
		await this.Users.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("email_address", emailAddress), CancellationToken);
	}
	
	#endregion
	
	#region Synchronization
	
	/// <summary>
	/// The index of a unique field covers the users of the declaring type and its descendants: it is replaced by another one
	/// when a descendant is created, and dropped when the field is no longer unique.
	/// </summary>
	[Fact]
	public async Task UniqueField_IndexFollowsTheTypeAndItsDescendants()
	{
		var field = UniqueField();
		var parent = await this.CreateUserTypeAsync(field);
		var parentSlug = parent["slug"]!.GetValue<string>();
		
		var index = Assert.Single(await this.GetUniqueIndexesAsync(field));
		Assert.StartsWith($"ux_{this._instance.MembershipId}_", index["name"].AsString);
		Assert.True(index["unique"].AsBoolean);
		Assert.Equal(["membership_id", field], index["key"].AsBsonDocument.Names);
		Assert.Equal(this._instance.MembershipId, index["partialFilterExpression"]["membership_id"].AsString);
		Assert.Equal([parentSlug], CoveredUserTypes(index));
		
		var child = await this.CreateUserTypeAsync(UniqueField(), baseType: parentSlug);
		var childSlug = child["slug"]!.GetValue<string>();
		
		var replaced = Assert.Single(await this.GetUniqueIndexesAsync(field));
		Assert.NotEqual(index["name"].AsString, replaced["name"].AsString);
		Assert.Equal(new[] { childSlug, parentSlug }.Order(), CoveredUserTypes(replaced));
		
		// The child no longer exists: the index covers the parent only again
		var userTypes = await this.UserTypesAsync();
		await userTypes.DeleteAsync(child["_id"]!.GetValue<string>());
		Assert.Equal([parentSlug], CoveredUserTypes(Assert.Single(await this.GetUniqueIndexesAsync(field))));
		
		// Not unique any more
		await userTypes.UpdateAsync(parent["_id"]!.GetValue<string>(), UserTypeBody(parent["name"]!.GetValue<string>(), field, isUnique: false));
		Assert.Empty(await this.GetUniqueIndexesAsync(field));
	}
	
	[Fact]
	public async Task DeletedType_DropsItsIndex()
	{
		var field = UniqueField();
		var userType = await this.CreateUserTypeAsync(field);
		Assert.Single(await this.GetUniqueIndexesAsync(field));
		
		var userTypes = await this.UserTypesAsync();
		await userTypes.DeleteAsync(userType["_id"]!.GetValue<string>());
		
		Assert.Empty(await this.GetUniqueIndexesAsync(field));
	}
	
	/// <summary>
	/// A field can not become unique while the users already have duplicate values: the change is rejected and nothing is saved.
	/// </summary>
	[Fact]
	public async Task MakingAFieldUnique_WithDuplicateValues_IsRejected()
	{
		var field = UniqueField();
		var userType = await this.CreateUserTypeAsync(field, isUnique: false);
		var slug = userType["slug"]!.GetValue<string>();
		using (var first = await this.CreateUserAsync(slug, field, "same"))
		using (var second = await this.CreateUserAsync(slug, field, "same"))
		{
			await ResourceClient.AssertStatusAsync(first, HttpStatusCode.Created);
			await ResourceClient.AssertStatusAsync(second, HttpStatusCode.Created);
		}
		
		var userTypes = await this.UserTypesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var response = await adminClient.PutAsJsonAsync($"{userTypes.Url}/{userType["_id"]!.GetValue<string>()}", UserTypeBody(userType["name"]!.GetValue<string>(), field, isUnique: true), CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Conflict);
		Assert.Equal("UniqueFieldHasDuplicates", error!["errorCode"]!.GetValue<string>());
		Assert.Contains(field, error["message"]!.GetValue<string>());
		
		var stored = await userTypes.GetAsync(userType["_id"]!.GetValue<string>());
		Assert.False(stored["properties"]![field]!["isUnique"]?.GetValue<bool>() ?? false);
		Assert.Empty(await this.GetUniqueIndexesAsync(field));
	}
	
	/// <summary>
	/// A new type inheriting a unique field extends the index to its users: rejected as well when it would have duplicates
	/// (not possible through the API, as the type has no users yet; here the duplicates are written to the database directly).
	/// </summary>
	[Fact]
	public async Task CreatingAType_WhoseIndexWouldHaveDuplicates_IsRejected()
	{
		var field = UniqueField();
		var parent = await this.CreateUserTypeAsync(field);
		var parentSlug = parent["slug"]!.GetValue<string>();
		var childName = UniqueName("Child");
		var childSlug = childName.ToLowerInvariant().Replace(' ', '-');
		
		await this.Users.InsertManyAsync(
		[
			new BsonDocument { { "membership_id", this._instance.MembershipId }, { "user_type", childSlug }, { field, "same" } },
			new BsonDocument { { "membership_id", this._instance.MembershipId }, { "user_type", childSlug }, { field, "same" } }
		], cancellationToken: CancellationToken);
		
		var userTypes = await this.UserTypesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var response = await adminClient.PostAsJsonAsync(userTypes.Url, UserTypeBody(childName, UniqueField(), isUnique: false, baseType: parentSlug), CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Conflict);
		Assert.Equal("UniqueFieldHasDuplicates", error!["errorCode"]!.GetValue<string>());
		
		var matches = await userTypes.QueryAsync(new { where = new { slug = childSlug } });
		Assert.Empty(matches);
		Assert.Equal([parentSlug], CoveredUserTypes(Assert.Single(await this.GetUniqueIndexesAsync(field))));
		
		await this.Users.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("user_type", childSlug), CancellationToken);
	}
	
	#endregion
	
	#region Scope
	
	/// <summary>
	/// Unrelated types declaring the same unique field don't share the values; a type and its descendants do.
	/// </summary>
	[Fact]
	public async Task UniqueField_IsUniqueAmongTheDeclaringTypeAndItsDescendants()
	{
		var field = UniqueField();
		var typeA = (await this.CreateUserTypeAsync(field))["slug"]!.GetValue<string>();
		var typeB = (await this.CreateUserTypeAsync(field))["slug"]!.GetValue<string>();
		var childOfA = (await this.CreateUserTypeAsync(UniqueField(), baseType: typeA))["slug"]!.GetValue<string>();
		
		using var userOfA = await this.CreateUserAsync(typeA, field, "V-1");
		await ResourceClient.AssertStatusAsync(userOfA, HttpStatusCode.Created);
		
		using var userOfB = await this.CreateUserAsync(typeB, field, "V-1");
		await ResourceClient.AssertStatusAsync(userOfB, HttpStatusCode.Created);
		
		using var secondUserOfA = await this.CreateUserAsync(typeA, field, "V-1");
		await AssertUniqueConstraintErrorAsync(secondUserOfA, field);
		
		using var userOfChild = await this.CreateUserAsync(childOfA, field, "V-1");
		await AssertUniqueConstraintErrorAsync(userOfChild, field);
	}
	
	/// <summary>
	/// Concurrent writes with the same value all pass the uniqueness check; the index lets only one of them in, and the others
	/// get the same validation error as the uniqueness check.
	/// </summary>
	[Fact]
	public async Task ConcurrentUsers_WithTheSameValue_OnlyOneIsCreated()
	{
		var field = UniqueField();
		var slug = (await this.CreateUserTypeAsync(field))["slug"]!.GetValue<string>();
		
		var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => this.CreateUserAsync(slug, field, "same")));
		try
		{
			Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
			foreach (var response in responses.Where(x => x.StatusCode != HttpStatusCode.Created))
			{
				await AssertUniqueConstraintErrorAsync(response, field);
			}
		}
		finally
		{
			foreach (var response in responses)
			{
				response.Dispose();
			}
		}
	}
	
	#endregion
	
	#region Startup
	
	/// <summary>
	/// On startup the indexes are brought in line with the user types: a missing index is created, an obsolete one dropped.
	/// </summary>
	[Fact]
	public async Task Startup_SynchronizesTheIndexes()
	{
		var field = UniqueField();
		await this.CreateUserTypeAsync(field);
		var index = Assert.Single(await this.GetUniqueIndexesAsync(field));
		await this.Users.Indexes.DropOneAsync(index["name"].AsString, CancellationToken);
		
		var obsoleteName = $"ux_{ObjectId.GenerateNewId()}_0123abcd_{UniqueField()}";
		var obsoleteKeys = Builders<BsonDocument>.IndexKeys.Ascending("membership_id").Ascending("obsolete");
		await this.Users.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(obsoleteKeys, new CreateIndexOptions { Name = obsoleteName }), cancellationToken: CancellationToken);
		
		await using (var factory = new ErtisAuthFactory(this._mongo.ConnectionString, this._instance.Database.DatabaseNamespace.DatabaseName))
		{
			factory.UseKestrel(0);
			using var client = factory.CreateClient();
		}
		
		Assert.Equal(index["name"].AsString, Assert.Single(await this.GetUniqueIndexesAsync(field))["name"].AsString);
		var names = (await (await this.Users.Indexes.ListAsync(CancellationToken)).ToListAsync(CancellationToken)).Select(x => x["name"].AsString);
		Assert.DoesNotContain(obsoleteName, names);
	}
	
	/// <summary>
	/// Existing duplicates keep an index from being created; the application still starts, and the other indexes are created.
	/// </summary>
	[Fact]
	public async Task Startup_WithDuplicateEmailAddresses_StillStarts()
	{
		var databaseName = $"ertisauth-{Guid.NewGuid():N}";
		var database = new MongoClient(this._mongo.ConnectionString).GetDatabase(databaseName);
		try
		{
			var users = database.GetCollection<BsonDocument>("users");
			var membershipId = ObjectId.GenerateNewId().ToString();
			await users.InsertManyAsync(
			[
				new BsonDocument { { "membership_id", membershipId }, { "email_address", "same@example.com" }, { "username", "first" } },
				new BsonDocument { { "membership_id", membershipId }, { "email_address", "same@example.com" }, { "username", "second" } }
			], cancellationToken: CancellationToken);
			
			await using (var factory = new ErtisAuthFactory(this._mongo.ConnectionString, databaseName))
			{
				factory.UseKestrel(0);
				using var client = factory.CreateClient();
				using var response = await client.GetAsync("/healthcheck", CancellationToken);
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			}
			
			var names = (await (await users.Indexes.ListAsync(CancellationToken)).ToListAsync(CancellationToken)).Select(x => x["name"].AsString).ToArray();
			Assert.DoesNotContain("ux_email_address", names);
			Assert.Contains("ux_username", names);
		}
		finally
		{
			await database.Client.DropDatabaseAsync(databaseName, CancellationToken);
		}
	}
	
	#endregion
}
