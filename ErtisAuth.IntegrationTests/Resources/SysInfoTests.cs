using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Resources;

/// <summary>
/// Every resource with sys info records who created it and when, and who modified it last and when (UTC).
/// A sys info sent by the caller is ignored.
/// </summary>
public class SysInfoTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private static readonly string[] SysCollections = ["memberships", "roles", "applications", "users", "user-types", "providers", "mailhooks", "webhooks", "code-policies"];
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	/// <summary>
	/// Sent in every create and update body; must never be stored.
	/// </summary>
	private static JsonObject ForgedSys => new()
	{
		["created_at"] = "2000-01-01T00:00:00Z",
		["created_by"] = "forged",
		["modified_at"] = "2000-01-01T00:00:00Z",
		["modified_by"] = "forged"
	};
	
	public static TheoryData<string> Resources => new(ResourceCases.Keys.Order());
	
	#endregion
	
	#region Constructors
	
	public SysInfoTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Resource Cases
	
	/// <summary>
	/// Per resource: the url (relative to the membership, or absolute when starting with '/'), and the create and update bodies.
	/// </summary>
	private static readonly Dictionary<string, (string Url, Func<string, JsonObject> Create, Func<string, JsonObject> Update)> ResourceCases = new()
	{
		["roles"] = ("roles",
			name => new JsonObject { ["name"] = name, ["permissions"] = new JsonArray("users.read") },
			name => new JsonObject { ["name"] = name, ["permissions"] = new JsonArray("users.read", "roles.read") }),
		["applications"] = ("applications",
			name => new JsonObject { ["name"] = name, ["role"] = "admin" },
			name => new JsonObject { ["name"] = $"{name} Renamed", ["role"] = "admin" }),
		["user-types"] = ("user-types",
			name => new JsonObject { ["name"] = name, ["baseType"] = "user", ["properties"] = new JsonObject() },
			name => new JsonObject { ["name"] = name, ["baseType"] = "user", ["description"] = "Changed", ["properties"] = new JsonObject() }),
		["webhooks"] = ("webhooks",
			name => WebhookBody(name, 1),
			name => WebhookBody(name, 2)),
		["mailhooks"] = ("mailhooks",
			name => MailHookBody(name, "Welcome"),
			name => MailHookBody(name, "Changed")),
		["code-policies"] = ("code-policies",
			name => PolicyBody(name, 6),
			name => PolicyBody(name, 8)),
		["memberships"] = ("/memberships",
			name => MembershipBody(name, 3600),
			name => MembershipBody(name, 7200)),
		["users"] = ("users",
			name => UserBody(name, "Created"),
			name => UserBody(name, "Changed"))
	};
	
	private static JsonObject WebhookBody(string name, int tryCount) => new()
	{
		["name"] = name,
		["event"] = "UserCreated",
		["status"] = "passive",
		["request"] = new JsonObject { ["method"] = "POST", ["url"] = "http://localhost:1/hooks", ["body"] = new JsonObject() },
		["try_count"] = tryCount
	};
	
	private static JsonObject MailHookBody(string name, string subject) => new()
	{
		["name"] = name,
		["event"] = "UserCreated",
		["status"] = "passive",
		["mailProvider"] = "smtp",
		["mailSubject"] = subject,
		["mailTemplate"] = "<p>Welcome</p>",
		["fromName"] = "ErtisAuth",
		["fromAddress"] = "no-reply@example.com",
		["sendToUtilizer"] = false,
		["recipients"] = new JsonArray(new JsonObject { ["displayName"] = "Support", ["emailAddress"] = "support@example.com" })
	};
	
	private static JsonObject PolicyBody(string name, int length) => new()
	{
		["name"] = name,
		["length"] = length,
		["contains_letters"] = true,
		["contains_digits"] = true,
		["expires_in"] = 300
	};
	
	private static JsonObject MembershipBody(string name, int expiresIn) => new()
	{
		["name"] = name,
		["slug"] = name.ToLowerInvariant(),
		["secret_key"] = "sys-info-membership-secret-key-sys-info-membership",
		["expires_in"] = expiresIn,
		["refresh_token_expires_in"] = 86400,
		["hash_algorithm"] = "ARGON2ID",
		["encoding"] = "UTF-8",
		["default_language"] = "en",
		["user_activation"] = "passive"
	};
	
	private static JsonObject UserBody(string name, string lastname) => new()
	{
		["username"] = name.ToLowerInvariant(),
		["firstname"] = "Sys",
		["lastname"] = lastname,
		["email_address"] = $"{name.ToLowerInvariant()}@example.com",
		["password"] = "Sys-P@ssw0rd!",
		["role"] = "admin",
		["user_type"] = "user"
	};
	
	#endregion
	
	#region Helpers
	
	private string UrlOf(string resource)
	{
		var url = ResourceCases[resource].Url;
		return url.StartsWith('/') ? url : $"{this.MembershipUrl}/{url}";
	}
	
	private static async Task<JsonObject> SendAsync(HttpClient client, HttpMethod method, string url, JsonObject body)
	{
		body["sys"] = ForgedSys;
		using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
		using var response = await client.SendAsync(request, CancellationToken);
		var content = await response.Content.ReadAsStringAsync(CancellationToken);
		Assert.True(response.IsSuccessStatusCode, $"{method} {url} -> {(int) response.StatusCode}: {content}");
		return JsonNode.Parse(content)!.AsObject();
	}
	
	private async Task<JsonObject> GetAsync(HttpClient client, string resource, string id)
	{
		return (await client.GetFromJsonAsync<JsonObject>($"{this.UrlOf(resource)}/{id}", CancellationToken))!;
	}
	
	private static DateTime ReadTime(JsonNode? node) => DateTime.Parse(node!.GetValue<string>(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
	
	/// <summary>
	/// MongoDB stores milliseconds, so the stored time may be up to a millisecond earlier than the one taken before the request.
	/// </summary>
	private static void AssertTimeBetween(DateTime before, DateTime after, JsonNode? node)
	{
		var time = ReadTime(node);
		Assert.InRange(time, before.AddMilliseconds(-1), after);
	}
	
	private static string UniqueName() => $"Sys{Guid.NewGuid():N}"[..20];
	
	/// <summary>
	/// Creates and then updates one resource, and returns it as stored after the update, with the time bounds of both requests.
	/// </summary>
	private async Task<(JsonObject Created, JsonObject Updated, DateTime CreatedBefore, DateTime CreatedAfter, DateTime UpdatedBefore, DateTime UpdatedAfter)> CreateAndUpdateAsync(HttpClient client, string resource)
	{
		var (_, create, update) = ResourceCases[resource];
		var name = UniqueName();
		
		var createdBefore = DateTime.UtcNow;
		var created = await SendAsync(client, HttpMethod.Post, this.UrlOf(resource), create(name));
		var createdAfter = DateTime.UtcNow;
		var id = created["_id"]!.GetValue<string>();
		var stored = await this.GetAsync(client, resource, id);
		
		var updatedBefore = DateTime.UtcNow;
		await SendAsync(client, HttpMethod.Put, $"{this.UrlOf(resource)}/{id}", update(name));
		var updatedAfter = DateTime.UtcNow;
		var updated = await this.GetAsync(client, resource, id);
		
		return (stored, updated, createdBefore, createdAfter, updatedBefore, updatedAfter);
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[MemberData(nameof(Resources))]
	public async Task Create_SetsTheCreationInfo_Update_SetsTheModificationInfo(string resource)
	{
		var client = await this._instance.CreateAdminClientAsync();
		
		var result = await this.CreateAndUpdateAsync(client, resource);
		
		var createdSys = result.Created["sys"]!;
		AssertTimeBetween(result.CreatedBefore, result.CreatedAfter, createdSys["created_at"]);
		Assert.Equal(ErtisAuthInstance.AdminUsername, createdSys["created_by"]!.GetValue<string>());
		Assert.Null(createdSys["modified_at"]);
		Assert.Null(createdSys["modified_by"]);
		
		var updatedSys = result.Updated["sys"]!;
		Assert.Equal(ReadTime(createdSys["created_at"]), ReadTime(updatedSys["created_at"]));
		Assert.Equal(ErtisAuthInstance.AdminUsername, updatedSys["created_by"]!.GetValue<string>());
		AssertTimeBetween(result.UpdatedBefore, result.UpdatedAfter, updatedSys["modified_at"]);
		Assert.Equal(ErtisAuthInstance.AdminUsername, updatedSys["modified_by"]!.GetValue<string>());
	}
	
	/// <summary>
	/// Providers are created by the system (on the first listing) and updated by administrators.
	/// </summary>
	[Fact]
	public async Task Provider_IsCreatedByTheSystem_AndModifiedByTheUpdater()
	{
		var client = await this._instance.CreateAdminClientAsync();
		var providers = (await client.GetFromJsonAsync<JsonArray>($"{this.MembershipUrl}/providers", CancellationToken))!;
		var provider = providers.Single(x => x!["name"]!.GetValue<string>() == "Microsoft")!.AsObject();
		Assert.Equal("system", provider["sys"]!["created_by"]!.GetValue<string>());
		
		var before = DateTime.UtcNow;
		var body = new JsonObject { ["name"] = "Microsoft", ["description"] = UniqueName(), ["defaultRole"] = "admin", ["defaultUserType"] = "user" };
		await SendAsync(client, HttpMethod.Put, $"{this.MembershipUrl}/providers/{provider["_id"]!.GetValue<string>()}", body);
		var after = DateTime.UtcNow;
		
		var updated = (await client.GetFromJsonAsync<JsonObject>($"{this.MembershipUrl}/providers/{provider["_id"]!.GetValue<string>()}", CancellationToken))!;
		Assert.Equal(ReadTime(provider["sys"]!["created_at"]), ReadTime(updated["sys"]!["created_at"]));
		Assert.Equal("system", updated["sys"]!["created_by"]!.GetValue<string>());
		AssertTimeBetween(before, after, updated["sys"]!["modified_at"]);
		Assert.Equal(ErtisAuthInstance.AdminUsername, updated["sys"]!["modified_by"]!.GetValue<string>());
	}
	
	/// <summary>
	/// An application (Basic token) is recorded by its slug.
	/// </summary>
	[Fact]
	public async Task ResourceCreatedByAnApplication_RecordsTheApplicationSlug()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var application = (await adminClient.GetFromJsonAsync<JsonObject>($"{this.MembershipUrl}/applications/{this._instance.ApplicationId}", CancellationToken))!;
		var applicationClient = this._instance.CreateClient($"Basic {this._instance.ApplicationId}:{this._instance.ApplicationSecret}");
		
		var result = await this.CreateAndUpdateAsync(applicationClient, "roles");
		
		var slug = application["slug"]!.GetValue<string>();
		Assert.Equal(slug, result.Created["sys"]!["created_by"]!.GetValue<string>());
		Assert.Equal(slug, result.Updated["sys"]!["modified_by"]!.GetValue<string>());
	}
	
	/// <summary>
	/// The resources of the setup are created by the system.
	/// </summary>
	[Theory]
	[InlineData("memberships")]
	[InlineData("roles")]
	[InlineData("applications")]
	[InlineData("users")]
	public async Task SetupResources_AreCreatedByTheSystem(string collection)
	{
		var documents = await this._instance.Database.GetCollection<BsonDocument>(collection).Find(Builders<BsonDocument>.Filter.Eq("sys.created_by", "system")).ToListAsync(CancellationToken);
		
		Assert.NotEmpty(documents);
	}
	
	/// <summary>
	/// Changing the password (also the reset flow's set-password) is an update of the user.
	/// </summary>
	[Fact]
	public async Task ChangePassword_SetsTheModificationInfo()
	{
		var client = await this._instance.CreateAdminClientAsync();
		var created = await SendAsync(client, HttpMethod.Post, this.UrlOf("users"), UserBody(UniqueName(), "Created"));
		var id = created["_id"]!.GetValue<string>();
		
		var before = DateTime.UtcNow;
		using var response = await client.PutAsJsonAsync($"{this.MembershipUrl}/users/{id}/change-password", new { password = "Changed-P@ssw0rd!" }, CancellationToken);
		var after = DateTime.UtcNow;
		Assert.True(response.IsSuccessStatusCode, $"{(int) response.StatusCode}: {await response.Content.ReadAsStringAsync(CancellationToken)}");
		
		var updated = await this.GetAsync(client, "users", id);
		Assert.Equal(ReadTime(created["sys"]!["created_at"]), ReadTime(updated["sys"]!["created_at"]));
		AssertTimeBetween(before, after, updated["sys"]!["modified_at"]);
		Assert.Equal(ErtisAuthInstance.AdminUsername, updated["sys"]!["modified_by"]!.GetValue<string>());
	}
	
	/// <summary>
	/// Guard for future write paths: after creating and updating every kind of resource, no stored resource lacks
	/// the creation info, and no modification info is half set.
	/// </summary>
	[Fact]
	public async Task NoStoredResource_LacksTheSysInfo()
	{
		var client = await this._instance.CreateAdminClientAsync();
		foreach (var resource in ResourceCases.Keys)
		{
			await this.CreateAndUpdateAsync(client, resource);
		}
		
		await client.GetFromJsonAsync<JsonArray>($"{this.MembershipUrl}/providers", CancellationToken);
		
		foreach (var collection in SysCollections)
		{
			var documents = await this._instance.Database.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(CancellationToken);
			Assert.NotEmpty(documents);
			foreach (var document in documents)
			{
				var description = $"{collection} {document["_id"]}: {(document.Contains("sys") ? document["sys"].ToJson() : "no sys")}";
				Assert.True(document.Contains("sys") && document["sys"].IsBsonDocument, description);
				
				var sys = document["sys"].AsBsonDocument;
				Assert.True(sys.Contains("created_at") && sys["created_at"].IsValidDateTime, description);
				Assert.True(sys.Contains("created_by") && sys["created_by"].IsString && sys["created_by"].AsString.Length > 0, description);
				Assert.Equal(sys.Contains("modified_at"), sys.Contains("modified_by"));
				Assert.NotEqual("forged", sys.GetValue("created_by", BsonNull.Value).ToString());
			}
		}
	}
	
	#endregion
}
