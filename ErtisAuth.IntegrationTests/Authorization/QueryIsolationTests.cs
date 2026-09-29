using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Authorization;

/// <summary>
/// The _query and _aggregate endpoints of membership bound resources against real MongoDB: whatever the caller's
/// filter or pipeline contains, only documents of the route membership are returned.
/// The documents are inserted directly (a marker field identifies them), one in the own and one in another membership.
/// </summary>
public class QueryIsolationTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string OtherMembershipId = "other-membership";
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	/// <summary>
	/// Route segment and MongoDB collection of every membership bound resource with a _query endpoint.
	/// </summary>
	public static TheoryData<string, string> QueryableResources => new()
	{
		{ "users", "users" },
		{ "roles", "roles" },
		{ "applications", "applications" },
		{ "user-types", "user-types" },
		{ "webhooks", "webhooks" },
		{ "mailhooks", "mailhooks" },
		{ "code-policies", "code-policies" },
		{ "events", "events" },
		{ "active-tokens", "active_tokens" },
		{ "revoked-tokens", "revoked_tokens" }
	};
	
	#endregion
	
	#region Constructors
	
	public QueryIsolationTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// Inserts a document with the marker into the own and into the other membership.
	/// </summary>
	private async Task<(string OwnId, string OtherId)> InsertMarkedDocumentsAsync(string collection, string marker)
	{
		var own = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "name", $"Own {marker}" }, { "marker", marker }, { "membership_id", this._instance.MembershipId } };
		var other = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "name", $"Other {marker}" }, { "marker", marker }, { "membership_id", OtherMembershipId } };
		await this._instance.Database.GetCollection<BsonDocument>(collection).InsertManyAsync([own, other], cancellationToken: CancellationToken);
		return (own["_id"].ToString()!, other["_id"].ToString()!);
	}
	
	private async Task<JsonArray> QueryAsync(string resource, string body)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
		using var response = await adminClient.PostAsync($"{this.MembershipUrl}/{resource}/_query", content, CancellationToken);
		var page = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		return page!["items"]!.AsArray();
	}
	
	private async Task<HttpResponseMessage> AggregateAsync(string pipeline)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var content = new StringContent(pipeline, System.Text.Encoding.UTF8, "application/json");
		return await adminClient.PostAsync($"{this.MembershipUrl}/active-tokens/_aggregate", content, CancellationToken);
	}
	
	private static string[] IdsOf(JsonArray items) => items.Select(x => x!["_id"]!.ToString()).ToArray();
	
	#endregion
	
	#region Query
	
	[Theory]
	[MemberData(nameof(QueryableResources))]
	public async Task Query_ReturnsOnlyTheDocumentsOfTheMembership(string resource, string collection)
	{
		var marker = Guid.NewGuid().ToString("N");
		var (ownId, otherId) = await this.InsertMarkedDocumentsAsync(collection, marker);
		
		string[] queries =
		[
			$$"""{ "where": { "marker": "{{marker}}" } }""",
			$$"""{ "where": { "marker": "{{marker}}", "membership_id": "{{OtherMembershipId}}" } }""",
			$$"""{ "where": { "marker": "{{marker}}", "membership_id": { "$ne": "{{this._instance.MembershipId}}" } } }""",
			$$"""{ "where": { "$or": [ { "membership_id": "{{OtherMembershipId}}" }, { "marker": "{{marker}}" } ] } }""",
			$$"""{ "where": { "$nor": [ { "membership_id": "{{this._instance.MembershipId}}" } ], "marker": "{{marker}}" } }""",
			$$"""{ "where": { "marker": "{{marker}}" }, "select": { "membership_id": 0 } }"""
		];
		
		// $expr is not supported on resources with hidden fields (users, applications)
		if (resource is not ("users" or "applications"))
		{
			queries = [..queries, $$"""{ "where": { "$expr": { "$eq": [ "$marker", "{{marker}}" ] } } }"""];
		}
		
		foreach (var query in queries)
		{
			var ids = IdsOf(await this.QueryAsync(resource, query));
			Assert.DoesNotContain(otherId, ids);
			Assert.All(ids, x => Assert.Equal(ownId, x));
		}
		
		// The filter itself works: the own document is found
		Assert.Equal([ownId], IdsOf(await this.QueryAsync(resource, $$"""{ "where": { "marker": "{{marker}}" } }""")));
	}
	
	[Theory]
	[InlineData("not json")]
	[InlineData("""{ "where": "not a document" }""")]
	public async Task Query_WithInvalidQuery_ReturnsBadRequest(string body)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
		using var response = await adminClient.PostAsync($"{this.MembershipUrl}/roles/_query", content, CancellationToken);
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
	}
	
	/// <summary>
	/// Unexpected errors (here the database rejecting the regular expression) answer with a generic message:
	/// internal details stay in the log.
	/// </summary>
	[Fact]
	public async Task Query_FailingInTheDatabase_DoesNotDiscloseTheError()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var content = new StringContent("""{ "where": { "name": { "$regex": "(" } } }""", System.Text.Encoding.UTF8, "application/json");
		using var response = await adminClient.PostAsync($"{this.MembershipUrl}/roles/_query", content, CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.InternalServerError);
		Assert.Equal("An unexpected error occurred", error!["message"]!.GetValue<string>());
		Assert.Equal("UnhandledExceptionError", error["errorCode"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Aggregate
	
	[Fact]
	public async Task Aggregate_ReturnsOnlyTheDocumentsOfTheMembership()
	{
		var marker = Guid.NewGuid().ToString("N");
		var (ownId, otherId) = await this.InsertMarkedDocumentsAsync("active_tokens", marker);
		
		using var matchResponse = await this.AggregateAsync($$"""[ { "$match": { "marker": "{{marker}}" } } ]""");
		var matched = await ResourceClient.AssertStatusAsync(matchResponse, HttpStatusCode.OK);
		var matchedJson = matched!.ToJsonString();
		Assert.Contains(ownId, matchedJson);
		Assert.DoesNotContain(otherId, matchedJson);
		
		using var groupResponse = await this.AggregateAsync("""[ { "$group": { "_id": "$membership_id", "count": { "$sum": 1 } } } ]""");
		var grouped = await ResourceClient.AssertStatusAsync(groupResponse, HttpStatusCode.OK);
		Assert.DoesNotContain(OtherMembershipId, grouped!.ToJsonString());
		
		using var facetResponse = await this.AggregateAsync($$"""[ { "$facet": { "all": [ { "$match": { "marker": "{{marker}}" } } ] } } ]""");
		var faceted = await ResourceClient.AssertStatusAsync(facetResponse, HttpStatusCode.OK);
		Assert.DoesNotContain(otherId, faceted!.ToJsonString());
	}
	
	[Theory]
	[InlineData("""[ { "$unionWith": { "coll": "active_tokens" } } ]""")]
	[InlineData("""[ { "$lookup": { "from": "memberships", "pipeline": [], "as": "memberships" } } ]""")]
	[InlineData("""[ { "$facet": { "leak": [ { "$unionWith": "users" } ] } } ]""")]
	[InlineData("""[ { "$out": "stolen" } ]""")]
	[InlineData("""[ { "$merge": { "into": "stolen" } } ]""")]
	[InlineData("""[ { "$graphLookup": { "from": "users", "startWith": "$user_id", "connectFromField": "_id", "connectToField": "_id", "as": "users" } } ]""")]
	[InlineData("""[ { "$collStats": { "count": {} } } ]""")]
	[InlineData("""[ { "$currentOp": {} } ]""")]
	[InlineData("""[ { "$match": { "$where": "true" } } ]""")]
	[InlineData("""[ { "$project": { "x": { "$function": { "body": "function() { return 1; }", "args": [], "lang": "js" } } } } ]""")]
	[InlineData("""[ { "$group": { "_id": null, "x": { "$accumulator": { "init": "function() { return 0; }", "accumulate": "function(s) { return s; }", "accumulateArgs": [], "merge": "function(a, b) { return a; }", "lang": "js" } } } } ]""")]
	public async Task Aggregate_WithStageReadingOrWritingOtherData_IsRejected(string pipeline)
	{
		using var response = await this.AggregateAsync(pipeline);
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		var collections = await (await this._instance.Database.ListCollectionNamesAsync(cancellationToken: CancellationToken)).ToListAsync(CancellationToken);
		Assert.DoesNotContain("stolen", collections);
	}
	
	#endregion
	
	#region Secret Fields
	
	/// <summary>
	/// Filtering (or sorting) on a hidden field would disclose it bit by bit, even though the field is never returned:
	/// every way of reaching a hidden field is rejected.
	/// </summary>
	[Theory]
	[InlineData("users", """{ "where": { "password_hash": { "$regex": "^\\$" } } }""")]
	[InlineData("users", """{ "where": { "$and": [ { "username": "admin" }, { "password_hash": { "$exists": true } } ] } }""")]
	[InlineData("users", """{ "where": { "$or": [ { "password_hash.0": "x" } ] } }""")]
	[InlineData("users", """{ "where": { "$nor": [ { "password_hash": { "$not": { "$regex": "^a" } } } ] } }""")]
	[InlineData("users", """{ "where": { "$expr": { "$regexMatch": { "input": "$password_hash", "regex": "^a" } } } }""")]
	[InlineData("users", """{ "where": { "$expr": { "$regexMatch": { "input": { "$getField": "password_hash" }, "regex": "^a" } } } }""")]
	[InlineData("users", """{ "where": { "$jsonSchema": { "patternProperties": { "^pass": { "pattern": "^a" } } } } }""")]
	[InlineData("users", """{ "where": { "$where": "this.password_hash.length > 5" } }""")]
	[InlineData("applications", """{ "where": { "secret_hash": { "$regex": "^a" } } }""")]
	[InlineData("applications", """{ "where": { "$expr": { "$eq": [ { "$substr": [ "$secret_hash", 0, 1 ] }, "a" ] } } }""")]
	[InlineData("roles", """{ "where": { "$where": "true" } }""")]
	[InlineData("roles", """{ "where": { "$expr": { "$function": { "body": "function() { return true; }", "args": [], "lang": "js" } } } }""")]
	public async Task Query_ReachingAHiddenFieldOrRunningJavaScript_IsRejected(string resource, string body)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
		using var response = await adminClient.PostAsync($"{this.MembershipUrl}/{resource}/_query", content, CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("InvalidQuery", error!["errorCode"]!.GetValue<string>());
	}
	
	[Theory]
	[InlineData("users", "password_hash")]
	[InlineData("users", "-password_hash")]
	[InlineData("applications", "secret_hash")]
	public async Task Sorting_ByAHiddenField_IsRejected(string resource, string sort)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var listResponse = await adminClient.GetAsync($"{this.MembershipUrl}/{resource}?sort={Uri.EscapeDataString(sort)}", CancellationToken);
		await ResourceClient.AssertStatusAsync(listResponse, HttpStatusCode.BadRequest);
		
		using var content = new StringContent("""{ "where": {} }""", System.Text.Encoding.UTF8, "application/json");
		using var queryResponse = await adminClient.PostAsync($"{this.MembershipUrl}/{resource}/_query?sort={Uri.EscapeDataString(sort)}", content, CancellationToken);
		await ResourceClient.AssertStatusAsync(queryResponse, HttpStatusCode.BadRequest);
	}
	
	[Fact]
	public async Task Query_NeverReturnsSecretFields()
	{
		var users = await this.QueryAsync("users", """{ "where": {}, "select": { "password_hash": 1, "username": 1 } }""");
		Assert.NotEmpty(users);
		Assert.All(users, x => Assert.Null(x!["password_hash"]));
		
		var applications = await this.QueryAsync("applications", """{ "where": {}, "select": { "secret_hash": 1, "name": 1 } }""");
		Assert.NotEmpty(applications);
		Assert.All(applications, x => Assert.Null(x!["secret_hash"]));
	}
	
	#endregion
}