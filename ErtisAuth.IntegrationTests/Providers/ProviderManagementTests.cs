using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;

namespace ErtisAuth.IntegrationTests.Providers;

/// <summary>
/// The providers of a membership (created inactive on the first listing) and their configuration.
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
	
	public ProviderManagementTests(OAuthErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Providers_AreCreatedForTheMembership()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		var providers = (await adminClient.GetFromJsonAsync<JsonArray>(this.ProvidersUrl, CancellationToken))!;
		
		Assert.Equal(["Apple", "AppleNative", "Facebook", "Google", "Microsoft"], providers.Select(x => x!["name"]!.GetValue<string>()).Order());
		Assert.All(providers, x => Assert.Equal(this._instance.MembershipId, x!["membership_id"]!.GetValue<string>()));
		
		var id = providers[0]!["_id"]!.GetValue<string>();
		using var getResponse = await adminClient.GetAsync($"{this.ProvidersUrl}/{id}", CancellationToken);
		var provider = await ResourceClient.AssertStatusAsync(getResponse, HttpStatusCode.OK);
		Assert.Equal(id, provider!["_id"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task GetProvider_WithUnknownId_ReturnsProviderNotFound()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.GetAsync($"{this.ProvidersUrl}/{ObjectId.GenerateNewId()}", CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NotFound);
		Assert.Equal("ProviderNotFound", error!["errorCode"]!.GetValue<string>());
	}
	
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
		Assert.Equal(privateKey, (await this._instance.GetProviderAsync("AppleNative"))["privateKey"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UpdateProvider_WithUnknownName_ReturnsBadRequest()
	{
		var provider = await this._instance.GetProviderAsync("Microsoft");
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.ProvidersUrl}/{provider["_id"]!.GetValue<string>()}", new { name = "Twitter" }, CancellationToken);
		
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("UnknownProvider", error!["errorCode"]!.GetValue<string>());
	}
	
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
		
		var names = providers.Select(x => x!["name"]!.GetValue<string>()).ToArray();
		Assert.Contains("Apple", names);
		Assert.DoesNotContain("Facebook", names);
		Assert.DoesNotContain("PRIVATE KEY", providers.ToJsonString());
		Assert.DoesNotContain("privateKey", providers.ToJsonString());
	}
	
	#endregion
}