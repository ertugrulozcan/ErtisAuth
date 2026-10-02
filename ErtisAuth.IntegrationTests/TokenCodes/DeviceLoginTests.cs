using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;

namespace ErtisAuth.IntegrationTests.TokenCodes;

/// <summary>
/// Device login (e.g. a smart TV) end to end: the device gets a code, the user approves it, the device polls the anonymous
/// generate-token endpoint. Also covers the BSON mapping of the token stored in the code (BearerTokenClassMap).
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
	
	#region Helpers
	
	/// <summary>
	/// The policy is inserted directly: the administrator role has no code-policies permission (see
	/// ResourceRequestTests.CodePolicy_CreateAndUpdate_WithoutIdAndMembership).
	/// </summary>
	private async Task EnableTokenCodesAsync()
	{
		await this._instance.Database.GetCollection<BsonDocument>("code-policies").InsertOneAsync(new BsonDocument
		{
			{ "name", "TV Code" },
			{ "slug", "tv-code" },
			{ "length", 8 },
			{ "contains_letters", true },
			{ "contains_digits", true },
			{ "expires_in", 300 },
			{ "membership_id", this._instance.MembershipId }
		}, cancellationToken: TestContext.Current.CancellationToken);
		
		await this._instance.UpdateMembershipAsync(membership => membership["code_policy"] = "tv-code");
	}
	
	private async Task<HttpResponseMessage> PollAsync(string code)
	{
		return await this._instance.CreateClient().GetAsync($"/memberships/{this._instance.MembershipId}/codes/generate-token/{code}", TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task DeviceLogin_FromTheCodeToASingleUseToken()
	{
		await this.EnableTokenCodesAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		// The device gets a code
		using var codeResponse = await adminClient.PostAsync($"/memberships/{this._instance.MembershipId}/codes", null, TestContext.Current.CancellationToken);
		var tokenCode = await ErtisAuthInstance.ReadJsonAsync(codeResponse);
		Assert.True(codeResponse.IsSuccessStatusCode, tokenCode.ToString());
		var code = tokenCode.GetProperty("code").GetString()!;
		Assert.Equal(8, code.Length);
		
		// Polling before the approval
		using (var response = await this.PollAsync(code))
		{
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}
		
		// The user approves the code
		using (var response = await adminClient.GetAsync($"/memberships/{this._instance.MembershipId}/codes/approve/{code}", TestContext.Current.CancellationToken))
		{
			Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
		}
		
		// The device gets a working token (read back from MongoDB through the BearerToken class map)
		string accessToken;
		using (var response = await this.PollAsync(code))
		{
			var token = await ErtisAuthInstance.ReadJsonAsync(response);
			Assert.True(response.IsSuccessStatusCode, token.ToString());
			accessToken = token.GetProperty("access_token").GetString()!;
		}
		
		var me = await this._instance.CreateClient($"Bearer {accessToken}").GetFromJsonAsync<JsonObject>("/me", TestContext.Current.CancellationToken);
		Assert.Equal(ErtisAuthInstance.AdminUsername, me!["username"]!.GetValue<string>());
		
		// Only once
		using var secondPoll = await this.PollAsync(code);
		Assert.Equal(HttpStatusCode.Unauthorized, secondPoll.StatusCode);
	}
	
	#endregion
}