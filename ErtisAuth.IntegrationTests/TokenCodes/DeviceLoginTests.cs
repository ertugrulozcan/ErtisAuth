using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.TokenCodes;

/// <summary>
/// Device login (e.g. a smart TV) end to end: the device gets a user code and a device code, the user approves the user
/// code, the device polls the anonymous token endpoint with its device code. The user code alone never gives a token.
/// </summary>
public class DeviceLoginTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public DeviceLoginTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Properties
	
	private string CodesUrl => $"/memberships/{this._instance.MembershipId}/codes";
	
	private IMongoCollection<BsonDocument> Codes => this._instance.Database.GetCollection<BsonDocument>("codes");
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// The policy is inserted directly: the administrator role has no code-policies permission (see
	/// ResourceRequestTests.CodePolicy_CreateAndUpdate_WithoutIdAndMembership).
	/// </summary>
	private async Task EnableTokenCodesAsync()
	{
		var policy = new BsonDocument
		{
			{ "name", "TV Code" },
			{ "slug", "tv-code" },
			{ "length", 8 },
			{ "contains_letters", true },
			{ "contains_digits", true },
			{ "expires_in", 300 },
			{ "membership_id", this._instance.MembershipId }
		};
		
		await this._instance.Database.GetCollection<BsonDocument>("code-policies").ReplaceOneAsync(
			Builders<BsonDocument>.Filter.Eq("slug", "tv-code") & Builders<BsonDocument>.Filter.Eq("membership_id", this._instance.MembershipId),
			policy,
			new ReplaceOptions { IsUpsert = true },
			TestContext.Current.CancellationToken);
		
		await this._instance.UpdateMembershipAsync(membership => membership["code_policy"] = "tv-code");
	}
	
	private async Task<(string UserCode, string DeviceCode)> CreateCodeAsync(HttpClient client)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, this.CodesUrl);
		request.Headers.Add("X-IpAddress", "203.0.113.42");
		request.Headers.Add("X-UserAgent", "SmartTV/1.0");
		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		var tokenCode = await ErtisAuthInstance.ReadJsonAsync(response);
		Assert.True(response.StatusCode == HttpStatusCode.Created, tokenCode.ToString());
		
		return (tokenCode.GetProperty("user_code").GetString()!, tokenCode.GetProperty("device_code").GetString()!);
	}
	
	private async Task<HttpResponseMessage> PollAsync(string deviceCode)
	{
		return await this._instance.CreateClient().PostAsJsonAsync($"{this.CodesUrl}/token", new { device_code = deviceCode }, TestContext.Current.CancellationToken);
	}
	
	/// <summary>
	/// Lets the next poll through without waiting for the interval.
	/// </summary>
	private async Task WaitForTheIntervalAsync(string userCode)
	{
		await this.Codes.UpdateOneAsync(
			Builders<BsonDocument>.Filter.Eq("user_code", userCode),
			Builders<BsonDocument>.Update.Unset("last_polled_at"),
			cancellationToken: TestContext.Current.CancellationToken);
	}
	
	private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
	{
		var error = await ErtisAuthInstance.ReadJsonAsync(response);
		return error.ValueKind == JsonValueKind.Object && error.TryGetProperty("errorCode", out var errorCode) ? errorCode.GetString() : null;
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task DeviceLogin_FromTheCodeToASingleUseToken()
	{
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		// The device gets a code
		var (userCode, deviceCode) = await this.CreateCodeAsync(adminClient);
		Assert.Equal(8, userCode.Length);
		
		// Polling before the approval
		using (var response = await this.PollAsync(deviceCode))
		{
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
			Assert.Equal("UnauthorizedTokenCode", await ReadErrorCodeAsync(response));
		}
		
		// The approval screen shows the device; the code may be typed in lower case
		var shown = await adminClient.GetFromJsonAsync<JsonObject>($"{this.CodesUrl}/{userCode.ToLowerInvariant()}", TestContext.Current.CancellationToken);
		Assert.Equal("SmartTV/1.0", shown!["client_info"]!["user_agent"]!.GetValue<string>());
		Assert.Equal("pending", shown["status"]!.GetValue<string>());
		
		// The user approves the code
		using (var response = await adminClient.PostAsync($"{this.CodesUrl}/{userCode}/approve", null, TestContext.Current.CancellationToken))
		{
			Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
		}
		
		// The device gets a working token
		await this.WaitForTheIntervalAsync(userCode);
		string accessToken;
		using (var response = await this.PollAsync(deviceCode))
		{
			var token = await ErtisAuthInstance.ReadJsonAsync(response);
			Assert.True(response.StatusCode == HttpStatusCode.Created, token.ToString());
			accessToken = token.GetProperty("access_token").GetString()!;
		}
		
		var me = await this._instance.CreateClient($"Bearer {accessToken}").GetFromJsonAsync<JsonObject>("/me", TestContext.Current.CancellationToken);
		Assert.Equal(ErtisAuthInstance.AdminUsername, me!["username"]!.GetValue<string>());
		
		// The session shows the device
		var session = await this._instance.Database.GetCollection<BsonDocument>("active_tokens")
			.Find(Builders<BsonDocument>.Filter.Eq("access_token", accessToken))
			.SingleAsync(TestContext.Current.CancellationToken);
		Assert.Equal("SmartTV/1.0", session["client_info"]["user_agent"].AsString);
		Assert.Equal("203.0.113.42", session["client_info"]["ip_address"].AsString);
		
		// Only once
		using var secondPoll = await this.PollAsync(deviceCode);
		Assert.Equal(HttpStatusCode.Unauthorized, secondPoll.StatusCode);
		Assert.Equal("InvalidToken", await ReadErrorCodeAsync(secondPoll));
	}
	
	[Fact]
	public async Task UserCode_ShownOnTheScreen_GetsNoToken()
	{
		// Attack: someone who sees the code on the device polls with it, to get the token before the device
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var (userCode, _) = await this.CreateCodeAsync(adminClient);
		using (await adminClient.PostAsync($"{this.CodesUrl}/{userCode}/approve", null, TestContext.Current.CancellationToken))
		{
		}
		
		using var response = await this.PollAsync(userCode);
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("InvalidToken", await ReadErrorCodeAsync(response));
	}
	
	[Fact]
	public async Task StoredCode_HasNeitherTheDeviceCodeNorAToken()
	{
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var (userCode, deviceCode) = await this.CreateCodeAsync(adminClient);
		using (await adminClient.PostAsync($"{this.CodesUrl}/{userCode}/approve", null, TestContext.Current.CancellationToken))
		{
		}
		
		var stored = await this.Codes.Find(Builders<BsonDocument>.Filter.Eq("user_code", userCode)).SingleAsync(TestContext.Current.CancellationToken);
		
		Assert.Equal("approved", stored["status"].AsString);
		Assert.False(stored.Contains("token"));
		Assert.True(stored.Contains("device_code_hash"));
		Assert.DoesNotContain(deviceCode, stored.ToJson());
	}
	
	[Fact]
	public async Task DeniedCode_GetsNoToken()
	{
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var (userCode, deviceCode) = await this.CreateCodeAsync(adminClient);
		
		using (var response = await adminClient.PostAsync($"{this.CodesUrl}/{userCode}/deny", null, TestContext.Current.CancellationToken))
		{
			Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
		}
		
		using var poll = await this.PollAsync(deviceCode);
		Assert.Equal(HttpStatusCode.Unauthorized, poll.StatusCode);
		Assert.Equal("TokenCodeDenied", await ReadErrorCodeAsync(poll));
		
		// A denied code can't be approved afterwards
		using var approval = await adminClient.PostAsync($"{this.CodesUrl}/{userCode}/approve", null, TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.Conflict, approval.StatusCode);
	}
	
	[Fact]
	public async Task PollingTooOften_AnswersSlowDown()
	{
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var (_, deviceCode) = await this.CreateCodeAsync(adminClient);
		using (await this.PollAsync(deviceCode))
		{
		}
		
		using var response = await this.PollAsync(deviceCode);
		
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("TokenCodeSlowDown", await ReadErrorCodeAsync(response));
	}
	
	[Fact]
	public async Task ApprovedWithAScopedToken_TheDeviceGetsTheSameScopes()
	{
		// Regression: a scoped token approved a device, which got an unscoped token of the user (a token can only be narrowed)
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var (userCode, deviceCode) = await this.CreateCodeAsync(adminClient);
		
		using var scopedTokenRequest = new HttpRequestMessage(HttpMethod.Post, "/generate-token");
		scopedTokenRequest.Headers.Add("Membership", this._instance.MembershipId);
		scopedTokenRequest.Headers.Authorization = adminClient.DefaultRequestHeaders.Authorization;
		scopedTokenRequest.Content = JsonContent.Create(new { scopes = new[] { "tokens.create", "users.read" } });
		using var scopedTokenResponse = await this._instance.CreateClient().SendAsync(scopedTokenRequest, TestContext.Current.CancellationToken);
		var scopedToken = (await ErtisAuthInstance.ReadJsonAsync(scopedTokenResponse)).GetProperty("access_token").GetString()!;
		
		using (var response = await this._instance.CreateClient($"Bearer {scopedToken}").PostAsync($"{this.CodesUrl}/{userCode}/approve", null, TestContext.Current.CancellationToken))
		{
			Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
		}
		
		string deviceToken;
		using (var response = await this.PollAsync(deviceCode))
		{
			var token = await ErtisAuthInstance.ReadJsonAsync(response);
			Assert.True(response.StatusCode == HttpStatusCode.Created, token.ToString());
			deviceToken = token.GetProperty("access_token").GetString()!;
		}
		
		var deviceClient = this._instance.CreateClient($"Bearer {deviceToken}");
		using (var users = await deviceClient.GetAsync($"/memberships/{this._instance.MembershipId}/users", TestContext.Current.CancellationToken))
		{
			Assert.Equal(HttpStatusCode.OK, users.StatusCode);
		}
		
		using var roles = await deviceClient.GetAsync($"/memberships/{this._instance.MembershipId}/roles", TestContext.Current.CancellationToken);
		Assert.Equal(HttpStatusCode.Forbidden, roles.StatusCode);
	}
	
	[Fact]
	public async Task ApprovingWithABasicToken_IsRejected()
	{
		// The device is signed in as the user who approves the code, so an application can't approve it
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		var (userCode, _) = await this.CreateCodeAsync(adminClient);
		var applicationClient = this._instance.CreateClient($"Basic {this._instance.ApplicationId}:{this._instance.ApplicationSecret}");
		
		using var response = await applicationClient.PostAsync($"{this.CodesUrl}/{userCode}/approve", null, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Equal("TokenTypeNotSupported", await ReadErrorCodeAsync(response));
	}
	
	#endregion
}
