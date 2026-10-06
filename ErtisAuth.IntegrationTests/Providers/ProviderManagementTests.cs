using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Providers;

/// <summary>
/// The providers of a membership: created by type (POST), configured (PUT), listed and deleted. The "type" is the
/// discriminator of the concrete provider both in the API (JSON) and in the database (BSON).
/// </summary>
public class ProviderManagementTests : IClassFixture<OAuthErtisAuthInstance>
{
	#region Fields
	
	private readonly OAuthErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string ProvidersUrl => $"/memberships/{this._instance.MembershipId}/providers";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public ProviderManagementTests(OAuthErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// An active provider of the given type with every setting the type has (slug 'created-{type}').
	/// </summary>
	private static JsonObject CreateModel(string type)
	{
		var model = new JsonObject
		{
			["type"] = type,
			["slug"] = $"created-{type.ToLowerInvariant()}",
			["defaultRole"] = "admin",
			["defaultUserType"] = "user",
			["isActive"] = true,
			["appClientId"] = $"{type.ToLowerInvariant()}-client-id"
		};
		
		if (type == "Microsoft")
		{
			model["tenantId"] = "tenant-id";
		}
		
		if (type is "Apple" or "AppleNative")
		{
			model["teamId"] = "TEAM123456";
			model["privateKeyId"] = "KEY1234567";
			model["privateKey"] = FakeOAuthProviders.CreateApplePrivateKeyPem();
			model["redirectUri"] = "https://app.example.com/apple/callback";
		}
		
		return model;
	}
	
	private static void AssertSettings(JsonObject expected, JsonNode? actual)
	{
		Assert.NotNull(actual);
		foreach (var (name, value) in expected)
		{
			Assert.True(JsonNode.DeepEquals(value, actual[name]), $"{name}: expected {value?.ToJsonString()}, actual {actual[name]?.ToJsonString()}");
		}
	}
	
	private async Task<HttpResponseMessage> CreateAsync(object model)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync(this.ProvidersUrl, model, CancellationToken);
	}
	
	#endregion
	
	#region Create
	
	/// <summary>
	/// The created provider is answered and listed with its type and its type's settings, and stored with the "type" discriminator.
	/// </summary>
	[Theory]
	[InlineData("Apple")]
	[InlineData("AppleNative")]
	[InlineData("Facebook")]
	[InlineData("Google")]
	[InlineData("Microsoft")]
	public async Task CreateProvider_KeepsTheTypeAndItsSettings(string type)
	{
		var model = CreateModel(type);
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await this.CreateAsync(model);
		
		var created = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		var id = created!["_id"]!.GetValue<string>();
		Assert.EndsWith($"{this.ProvidersUrl}/{id}", response.Headers.Location?.ToString());
		Assert.Equal(type, created["name"]!.GetValue<string>());
		AssertSettings(model, created);
		
		AssertSettings(model, await adminClient.GetFromJsonAsync<JsonObject>($"{this.ProvidersUrl}/{id}", CancellationToken));
		AssertSettings(model, await adminClient.GetFromJsonAsync<JsonObject>($"{this.ProvidersUrl}/{model["slug"]}", CancellationToken));
		
		// Regression: the list was serialized as the abstract Provider, without the type's settings
		var providers = await adminClient.GetFromJsonAsync<JsonArray>(this.ProvidersUrl, CancellationToken);
		AssertSettings(model, providers!.Single(x => x!["_id"]!.GetValue<string>() == id));
		
		var stored = await this._instance.Database.GetCollection<BsonDocument>("providers").Find(new BsonDocument("_id", ObjectId.Parse(id))).SingleAsync(CancellationToken);
		Assert.Equal(type, stored["type"].AsString);
		Assert.Equal(model["appClientId"]!.GetValue<string>(), stored["appClientId"].AsString);
	}
	
	[Fact]
	public async Task CreateProvider_OfTheSameTypeWithAnotherSlug_IsCreated()
	{
		await this._instance.ConfigureProviderAsync("Google", x => x["appClientId"] = "google-client-id");
		
		using var response = await this.CreateAsync(new { type = "Google", slug = $"google-{Guid.NewGuid():N}", isActive = false });
		
		var created = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		Assert.Equal("Google", created!["type"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task CreateProvider_WithAnExistingSlug_ReturnsProviderAlreadyExists()
	{
		await this._instance.ConfigureProviderAsync("Google", x => x["appClientId"] = "google-client-id");
		
		using var response = await this.CreateAsync(new { type = "Facebook", slug = OAuthErtisAuthInstance.SlugOf("Google"), isActive = false });
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Conflict);
		Assert.Equal("ProviderAlreadyExists", error!["errorCode"]!.GetValue<string>());
	}
	
	/// <summary>
	/// The slug is unique in the membership in the database too (the check of the service alone is not atomic).
	/// </summary>
	[Fact]
	public async Task Providers_HaveAUniqueSlugIndex()
	{
		var indexes = await (await this._instance.Database.GetCollection<BsonDocument>("providers").Indexes.ListAsync(CancellationToken)).ToListAsync(CancellationToken);
		
		var index = Assert.Single(indexes, x => x["key"].AsBsonDocument.Names.SequenceEqual(["membership_id", "slug"]));
		Assert.True(index.GetValue("unique", false).ToBoolean());
	}
	
	[Fact]
	public async Task CreateProvider_WithoutType_ReturnsProviderTypeRequired()
	{
		using var response = await this.CreateAsync(new { name = "Provider", isActive = false });
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("ProviderTypeRequired", error!["errorCode"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task CreateProvider_WithErtisAuthType_ReturnsUnsupportedProvider()
	{
		using var response = await this.CreateAsync(new { type = "ErtisAuth", isActive = false });
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("UnsupportedProvider", error!["errorCode"]!.GetValue<string>());
		Assert.Equal(400, error["statusCode"]!.GetValue<int>());
	}
	
	[Fact]
	public async Task CreateProvider_WithUnknownType_IsRejected()
	{
		using var response = await this.CreateAsync(new { type = "Twitter", isActive = false });
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
	}
	
	[Fact]
	public async Task CreateProvider_ActiveWithoutSettings_IsRejected()
	{
		using var response = await this.CreateAsync(new { type = "Apple", slug = "unconfigured-apple", isActive = true, defaultRole = "admin", defaultUserType = "user" });
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Contains("Private key is required", error!.ToJsonString());
	}
	
	#endregion
	
	#region Read
	
	[Fact]
	public async Task GetProvider_WithUnknownId_ReturnsProviderNotFound()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.GetAsync($"{this.ProvidersUrl}/{ObjectId.GenerateNewId()}", CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NotFound);
		Assert.Equal("ProviderNotFound", error!["errorCode"]!.GetValue<string>());
	}
	
	/// <summary>
	/// A provider stored before the provider types (master): readable once the "type" field is added by the rollout migration (type = name).
	/// </summary>
	[Fact]
	public async Task LegacyProvider_WithTheTypeAdded_IsReadable()
	{
		var id = ObjectId.GenerateNewId();
		await this._instance.Database.GetCollection<BsonDocument>("providers").InsertOneAsync(new BsonDocument
		{
			{ "_id", id },
			{ "name", "Facebook" },
			{ "slug", "legacy-facebook" },
			{ "appClientId", "legacy-app-id" },
			{ "defaultRole", "admin" },
			{ "defaultUserType", "user" },
			{ "isActive", false },
			{ "trust_email", true },
			{ "membership_id", this._instance.MembershipId },
			{ "type", "Facebook" }
		}, cancellationToken: CancellationToken);
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		var provider = await adminClient.GetFromJsonAsync<JsonObject>($"{this.ProvidersUrl}/{id}", CancellationToken);
		
		Assert.Equal("Facebook", provider!["type"]!.GetValue<string>());
		Assert.Equal("legacy-app-id", provider["appClientId"]!.GetValue<string>());
		Assert.True(provider["trust_email"]!.GetValue<bool>());
	}
	
	#endregion
	
	#region Update
	
	[Fact]
	public async Task UpdateProvider_OmittedFlagsKeepTheirValues()
	{
		var configured = await this._instance.ConfigureProviderAsync("Google", x =>
		{
			x["appClientId"] = "google-client-id";
			x["trust_email"] = true;
		});
		Assert.True(configured["isActive"]!.GetValue<bool>());
		Assert.True(configured["trust_email"]!.GetValue<bool>());
		
		var updated = await this._instance.ConfigureProviderAsync("Google", x =>
		{
			x.Remove("isActive");
			x["appClientId"] = "google-client-id-2";
		});
		Assert.True(updated["isActive"]!.GetValue<bool>());
		Assert.True(updated["trust_email"]!.GetValue<bool>());
		Assert.Equal("google-client-id-2", updated["appClientId"]!.GetValue<string>());
		
		var deactivated = await this._instance.ConfigureProviderAsync("Google", x => x["isActive"] = false);
		Assert.False(deactivated["isActive"]!.GetValue<bool>());
		Assert.Equal("google-client-id-2", deactivated["appClientId"]!.GetValue<string>());
	}
	
	/// <summary>
	/// The private key (Sign in with Apple) is kept when an update does not send it.
	/// </summary>
	[Fact]
	public async Task UpdateProvider_WithoutPrivateKey_KeepsTheCurrentKey()
	{
		var privateKey = FakeOAuthProviders.CreateApplePrivateKeyPem();
		await this._instance.ConfigureProviderAsync("AppleNative", x =>
		{
			x["appClientId"] = "com.example.native";
			x["teamId"] = "TEAM123456";
			x["privateKeyId"] = "KEY1234567";
			x["privateKey"] = privateKey;
			x["redirectUri"] = "https://app.example.com/apple/callback";
		});
		
		var updated = await this._instance.ConfigureProviderAsync("AppleNative", x =>
		{
			x["appClientId"] = "com.example.native";
			x["teamId"] = "TEAM654321";
			x["privateKeyId"] = "KEY1234567";
			x["redirectUri"] = "https://app.example.com/apple/callback";
		});
		
		Assert.Equal("TEAM654321", updated["teamId"]!.GetValue<string>());
		Assert.Equal(privateKey, updated["privateKey"]!.GetValue<string>());
		Assert.Equal(privateKey, (await this._instance.FindProviderAsync("AppleNative"))!["privateKey"]!.GetValue<string>());
	}
	
	/// <summary>
	/// Only the type's own settings changed (the shared fields are the same): not an identical document.
	/// </summary>
	[Fact]
	public async Task UpdateProvider_ChangesTheAppClientIdOnly()
	{
		await this._instance.ConfigureProviderAsync("Microsoft", x =>
		{
			x["appClientId"] = "microsoft-client-id";
			x["tenantId"] = "tenant-id";
		});
		
		var updated = await this._instance.ConfigureProviderAsync("Microsoft", x =>
		{
			x["appClientId"] = "microsoft-client-id-2";
			x["tenantId"] = "tenant-id";
		});
		
		Assert.Equal("microsoft-client-id-2", updated["appClientId"]!.GetValue<string>());
		Assert.Equal("microsoft-client-id-2", (await this._instance.FindProviderAsync("Microsoft"))!["appClientId"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UpdateProvider_BySlug_UpdatesTheProvider()
	{
		var provider = await this._instance.ConfigureProviderAsync("Facebook", x => x["appClientId"] = "facebook-app-id");
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.ProvidersUrl}/{OAuthErtisAuthInstance.SlugOf("Facebook")}", new { description = $"Updated {Guid.NewGuid():N}" }, CancellationToken);
		
		var updated = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		Assert.Equal(provider["_id"]!.GetValue<string>(), updated!["_id"]!.GetValue<string>());
		Assert.StartsWith("Updated", updated["description"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UpdateProvider_DoesNotChangeTheType()
	{
		var provider = await this._instance.ConfigureProviderAsync("Microsoft", x => x["appClientId"] = "microsoft-client-id");
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.ProvidersUrl}/{provider["_id"]!.GetValue<string>()}", new { type = "Google", description = $"Changed {Guid.NewGuid():N}" }, CancellationToken);
		
		var updated = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		Assert.Equal("Microsoft", updated!["type"]!.GetValue<string>());
		Assert.Equal("Microsoft", (await this._instance.FindProviderAsync("Microsoft"))!["type"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UpdateProvider_WithAnotherSlug_ReturnsProviderSlugCannotBeChanged()
	{
		var provider = await this._instance.ConfigureProviderAsync("Facebook", x => x["appClientId"] = "facebook-app-id");
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.ProvidersUrl}/{provider["_id"]!.GetValue<string>()}", new { slug = "renamed-facebook" }, CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("ProviderSlugCannotBeChanged", error!["errorCode"]!.GetValue<string>());
		Assert.NotNull(await this._instance.FindProviderAsync("Facebook"));
	}
	
	[Fact]
	public async Task UpdateProvider_WithUnknownId_ReturnsProviderNotFound()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.ProvidersUrl}/{ObjectId.GenerateNewId()}", new { isActive = false }, CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NotFound);
		Assert.Equal("ProviderNotFound", error!["errorCode"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Delete
	
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DeleteProvider_RemovesTheProvider(bool bySlug)
	{
		var slug = $"deleted-{Guid.NewGuid():N}";
		using var createResponse = await this.CreateAsync(new { type = "Google", slug, isActive = false });
		var id = (await ResourceClient.AssertStatusAsync(createResponse, HttpStatusCode.Created))!["_id"]!.GetValue<string>();
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.DeleteAsync($"{this.ProvidersUrl}/{(bySlug ? slug : id)}", CancellationToken);
		
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		using var getResponse = await adminClient.GetAsync($"{this.ProvidersUrl}/{id}", CancellationToken);
		await ResourceClient.AssertStatusAsync(getResponse, HttpStatusCode.NotFound);
	}
	
	[Fact]
	public async Task DeleteProvider_WithUnknownSlug_ReturnsProviderNotFound()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.DeleteAsync($"{this.ProvidersUrl}/unknown-provider", CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NotFound);
		Assert.Equal("ProviderNotFound", error!["errorCode"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Active Providers
	
	/// <summary>
	/// Public (sign in pages list the available providers): only active providers and no secrets.
	/// </summary>
	[Fact]
	public async Task ActiveProviders_ArePublicAndListNoSecrets()
	{
		await this._instance.ConfigureProviderAsync("Apple", x =>
		{
			x["appClientId"] = "com.example.app";
			x["teamId"] = "TEAM123456";
			x["privateKeyId"] = "KEY1234567";
			x["privateKey"] = FakeOAuthProviders.CreateApplePrivateKeyPem();
			x["redirectUri"] = "https://app.example.com/apple/callback";
		});
		await this._instance.ConfigureProviderAsync("Facebook", x => x["isActive"] = false);
		
		using var response = await this._instance.CreateClient().GetAsync($"{this.ProvidersUrl}/active-providers", CancellationToken);
		var providers = (await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK))!.AsArray();
		
		var appleId = (await this._instance.FindProviderAsync("Apple"))!["_id"]!.GetValue<string>();
		var facebookId = (await this._instance.FindProviderAsync("Facebook"))!["_id"]!.GetValue<string>();
		var apple = providers.Single(x => x!["_id"]!.GetValue<string>() == appleId);
		Assert.Equal("com.example.app", apple!["appClientId"]!.GetValue<string>());
		Assert.Equal(OAuthErtisAuthInstance.SlugOf("Apple"), apple["slug"]!.GetValue<string>());
		Assert.Equal("Apple", apple["type"]!.GetValue<string>());
		Assert.DoesNotContain(providers, x => x!["_id"]!.GetValue<string>() == facebookId);
		Assert.DoesNotContain("PRIVATE KEY", providers.ToJsonString());
		Assert.DoesNotContain("privateKey", providers.ToJsonString());
	}
	
	#endregion
}
