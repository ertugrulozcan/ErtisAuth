using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;

namespace ErtisAuth.IntegrationTests.Authorization;

/// <summary>
/// The search endpoints of membership bound resources ($text on the collection's text index) only return documents
/// of the route membership. Regression guard: roles and applications were searched on the whole collection
/// (hidden until now only because those collections had no text index, so search failed with 500).
/// </summary>
public class SearchIsolationTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string OtherMembershipId = "other-membership";
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public SearchIsolationTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/{resource}");
	
	/// <summary>
	/// Inserted directly, as another membership's document matching the same keyword.
	/// </summary>
	private async Task<string> InsertOtherMembershipDocumentAsync(string collection, BsonDocument document)
	{
		var id = ObjectId.GenerateNewId();
		document["_id"] = id;
		document["membership_id"] = OtherMembershipId;
		await this._instance.Database.GetCollection<BsonDocument>(collection).InsertOneAsync(document, cancellationToken: TestContext.Current.CancellationToken);
		return id.ToString();
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task RoleSearch_ReturnsOnlyTheMembershipsRoles()
	{
		var roles = await this.CreateResourceClientAsync("roles");
		var keyword = $"Role{Guid.NewGuid():N}";
		var ownId = (await roles.CreateAsync(new { name = $"{keyword} Editor", permissions = Array.Empty<string>() }))["_id"]!.GetValue<string>();
		var otherId = await this.InsertOtherMembershipDocumentAsync("roles", new BsonDocument { { "name", $"{keyword} Editor" }, { "slug", $"{keyword.ToLowerInvariant()}-editor" } });
		
		var found = ResourceClient.IdsOf(await roles.SearchAsync(keyword));
		
		Assert.Equal([ownId], found);
		Assert.DoesNotContain(otherId, found);
	}
	
	[Fact]
	public async Task ApplicationSearch_ReturnsOnlyTheMembershipsApplications()
	{
		var applications = await this.CreateResourceClientAsync("applications");
		var keyword = $"App{Guid.NewGuid():N}";
		var ownId = (await applications.CreateAsync(new { name = $"{keyword} Worker", role = "admin" }))["_id"]!.GetValue<string>();
		var otherId = await this.InsertOtherMembershipDocumentAsync("applications", new BsonDocument { { "name", $"{keyword} Worker" }, { "slug", $"{keyword.ToLowerInvariant()}-worker" }, { "role", "admin" } });
		
		var found = ResourceClient.IdsOf(await applications.SearchAsync(keyword));
		
		Assert.Equal([ownId], found);
		Assert.DoesNotContain(otherId, found);
	}
	
	[Fact]
	public async Task UserSearch_ReturnsOnlyTheMembershipsUsers()
	{
		var users = await this.CreateResourceClientAsync("users");
		var keyword = $"User{Guid.NewGuid():N}";
		var ownId = (await users.CreateAsync(new
		{
			username = $"own{Guid.NewGuid():N}",
			firstname = "Own",
			lastname = keyword,
			email_address = $"own{Guid.NewGuid():N}@example.com",
			password = "Own-P@ssw0rd!",
			role = "admin",
			user_type = "user"
		}))["_id"]!.GetValue<string>();
		var otherId = await this.InsertOtherMembershipDocumentAsync("users", new BsonDocument { { "username", "other" }, { "firstname", "Other" }, { "lastname", keyword }, { "email_address", "other@example.com" }, { "role", "admin" }, { "user_type", "user" } });
		
		var found = ResourceClient.IdsOf(await users.SearchAsync(keyword));
		
		Assert.Equal([ownId], found);
		Assert.DoesNotContain(otherId, found);
	}
	
	[Fact]
	public async Task RoleSearch_IsCaseInsensitive()
	{
		var roles = await this.CreateResourceClientAsync("roles");
		var keyword = $"Case{Guid.NewGuid():N}";
		var id = (await roles.CreateAsync(new { name = $"{keyword} Editor", permissions = Array.Empty<string>() }))["_id"]!.GetValue<string>();
		
		Assert.Contains(id, ResourceClient.IdsOf(await roles.SearchAsync(keyword.ToLowerInvariant())));
		Assert.Contains(id, ResourceClient.IdsOf(await roles.SearchAsync(keyword.ToUpperInvariant())));
	}
	
	/// <summary>
	/// The keyword is a string value of the query, never query syntax: quotes and query fragments neither break
	/// the query (it used to fail with 500) nor widen it.
	/// </summary>
	[Theory]
	[InlineData("users")]
	[InlineData("roles")]
	[InlineData("applications")]
	public async Task Search_WithQuerySyntaxInTheKeyword_SearchesForTheText(string resource)
	{
		var client = await this.CreateResourceClientAsync(resource);
		
		foreach (var keyword in new[] { "a\"b", "back\\slash", "x\" } }, { \"membership_id\": { \"$ne\": \"\" } } ], \"$comment\": \"" })
		{
			var found = await client.SearchAsync(keyword);
			Assert.All(found, x => Assert.Equal(this._instance.MembershipId, x!["membership_id"]!.GetValue<string>()));
		}
	}
	
	[Fact]
	public async Task Search_WithoutKeyword_ReturnsBadRequest()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.GetAsync($"/memberships/{this._instance.MembershipId}/roles/search", TestContext.Current.CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
	}
	
	#endregion
}