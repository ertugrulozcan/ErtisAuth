using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Authorization;

/// <summary>
/// The HTTP pipeline (authentication handler, RBAC, membership isolation, error handling) with real tokens.
/// </summary>
public class MembershipIsolationTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public MembershipIsolationTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// Inserted directly, so the membership is not bound to the administrator's token in any way.
	/// </summary>
	private async Task<string> CreateOtherMembershipAsync()
	{
		var id = ObjectId.GenerateNewId();
		await this._instance.Database.GetCollection<BsonDocument>("memberships").InsertOneAsync(new BsonDocument
		{
			{ "_id", id },
			{ "name", "Other" },
			{ "slug", $"other-{id}" },
			{ "secret_key", "other-membership-secret-key-other-membership-secret" },
			{ "hash_algorithm", "ARGON2ID" },
			{ "expires_in", 3600 },
			{ "refresh_token_expires_in", 86400 }
		}, cancellationToken: TestContext.Current.CancellationToken);
		
		return id.ToString();
	}
	
	private static void AssertErrorBody(JsonElement error, HttpStatusCode statusCode)
	{
		Assert.False(string.IsNullOrEmpty(error.GetProperty("message").GetString()));
		Assert.False(string.IsNullOrEmpty(error.GetProperty("errorCode").GetString()));
		Assert.Equal((int) statusCode, error.GetProperty("statusCode").GetInt32());
		
		// No internals (stack traces, exception types) in error responses
		var json = error.ToString();
		Assert.DoesNotContain("   at ", json);
		Assert.DoesNotContain("Exception", json);
	}
	
	internal static void AssertWwwAuthenticate(HttpResponseMessage response)
	{
		var challenge = Assert.Single(response.Headers.WwwAuthenticate);
		Assert.Equal("Bearer", challenge.Scheme);
		Assert.Equal("realm=\"ErtisAuth\"", challenge.Parameter);
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task MembershipResources_WithoutToken_AreRejected()
	{
		// RFC 9110 §15.5.2: a 401 carries a WWW-Authenticate challenge
		using var response = await this._instance.CreateClient().GetAsync($"/memberships/{this._instance.MembershipId}/users", TestContext.Current.CancellationToken);
		var error = await ErtisAuthInstance.ReadJsonAsync(response);
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		AssertWwwAuthenticate(response);
		Assert.Equal("AuthorizationHeaderMissing", error.GetProperty("errorCode").GetString());
		AssertErrorBody(error, HttpStatusCode.Unauthorized);
	}
	
	[Fact]
	public async Task MembershipResources_WithTheOwnMembershipsToken_AreAccessible()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.GetAsync($"/memberships/{this._instance.MembershipId}/users", TestContext.Current.CancellationToken);
		var users = await ErtisAuthInstance.ReadJsonAsync(response);
		
		Assert.True(response.IsSuccessStatusCode, users.ToString());
		Assert.Contains(users.GetProperty("items").EnumerateArray(), x => x.GetProperty("username").GetString() == ErtisAuthInstance.AdminUsername);
	}
	
	[Fact]
	public async Task MembershipResources_OfAnotherMembership_AreForbidden()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var otherMembershipId = await this.CreateOtherMembershipAsync();
		
		using var response = await adminClient.GetAsync($"/memberships/{otherMembershipId}/users", TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		Assert.Empty(response.Headers.WwwAuthenticate);
		AssertErrorBody(await ErtisAuthInstance.ReadJsonAsync(response), HttpStatusCode.Forbidden);
	}
	
	[Fact]
	public async Task MembershipResources_WithAnInvalidToken_AreUnauthorized()
	{
		using var response = await this._instance.CreateClient("Bearer not-a-token").GetAsync($"/memberships/{this._instance.MembershipId}/users", TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		AssertWwwAuthenticate(response);
		AssertErrorBody(await ErtisAuthInstance.ReadJsonAsync(response), HttpStatusCode.Unauthorized);
	}
	
	[Fact]
	public async Task CreateMembership_WithoutAnId_Succeeds()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PostAsJsonAsync("/memberships", new
		{
			name = $"Created {Guid.NewGuid():N}",
			secret_key = "created-membership-secret-key-created-membership-secret",
			hash_algorithm = "ARGON2ID",
			expires_in = 3600,
			refresh_token_expires_in = 86400
		}, TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
	}
	
	[Fact]
	public async Task UpdateMembership_WithAnotherIdInTheBody_UpdatesOnlyTheRouteMembership()
	{
		// The permission is checked for the route id (RbacObject), so the body must not pick another membership
		var otherMembershipId = await this.CreateOtherMembershipAsync();
		var name = $"Renamed {Guid.NewGuid():N}";
		
		// A body that would pass the other membership's validation (own slug and secret key would conflict)
		await this._instance.UpdateMembershipAsync(membership =>
		{
			membership["_id"] = otherMembershipId;
			membership["name"] = name;
			membership["slug"] = name;
			membership.Remove("secret_key");
		});
		
		var memberships = this._instance.Database.GetCollection<BsonDocument>("memberships");
		var other = await memberships.Find(new BsonDocument("_id", ObjectId.Parse(otherMembershipId))).SingleAsync(TestContext.Current.CancellationToken);
		var own = await memberships.Find(new BsonDocument("_id", ObjectId.Parse(this._instance.MembershipId))).SingleAsync(TestContext.Current.CancellationToken);
		Assert.Equal("Other", other["name"].AsString);
		Assert.Equal(name, own["name"].AsString);
	}
	
	[Fact]
	public async Task NotFound_ReturnsTheErrorFormat()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.GetAsync($"/memberships/{this._instance.MembershipId}/users/5f8a1b2c3d4e5f6a7b8c9d99", TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		AssertErrorBody(await ErtisAuthInstance.ReadJsonAsync(response), HttpStatusCode.NotFound);
	}
	
	#endregion
}