using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

/// <summary>
/// Create and update requests carry only the resource's fields: the id and the membership come from the route.
/// Regression guard: the endpoints used to bind the domain models, whose non-nullable / C# required members
/// ('_id', 'membership_id', ...) became mandatory in the request with nullable reference types enabled.
/// </summary>
public class ResourceRequestTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public ResourceRequestTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static async Task<JsonObject> AssertSuccessAsync(HttpResponseMessage response)
	{
		var body = await ErtisAuthInstance.ReadJsonAsync(response);
		Assert.True(response.IsSuccessStatusCode, $"{(int) response.StatusCode}: {body}");
		return JsonNode.Parse(body.GetRawText())!.AsObject();
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task CodePolicy_CreateAndUpdate_WithoutIdAndMembership()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var createResponse = await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/code-policies", new { name = "Kiosk Code", length = 6, contains_digits = true, expires_in = 300 }, TestContext.Current.CancellationToken);
		var created = await AssertSuccessAsync(createResponse);
		Assert.Equal("kiosk-code", created["slug"]!.GetValue<string>());
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		
		var id = created["_id"]!.GetValue<string>();
		using var updateResponse = await adminClient.PutAsJsonAsync($"{this.MembershipUrl}/code-policies/{id}", new { name = "Kiosk Code", length = 8, contains_digits = true, expires_in = 300 }, TestContext.Current.CancellationToken);
		var updated = await AssertSuccessAsync(updateResponse);
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal(8, updated["length"]!.GetValue<int>());
	}
	
	[Fact]
	public async Task MailHook_CreateAndUpdate_WithoutIdAndMembership()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var mailHook = new
		{
			name = "Welcome Mail",
			@event = "UserCreated",
			status = "passive",
			mailProvider = "smtp",
			mailSubject = "Welcome",
			mailTemplate = "<p>Welcome</p>",
			fromName = "ErtisAuth",
			fromAddress = "no-reply@example.com",
			sendToUtilizer = true
		};
		
		using var createResponse = await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/mailhooks", mailHook, TestContext.Current.CancellationToken);
		var created = await AssertSuccessAsync(createResponse);
		
		var id = created["_id"]!.GetValue<string>();
		using var updateResponse = await adminClient.PutAsJsonAsync($"{this.MembershipUrl}/mailhooks/{id}", mailHook, TestContext.Current.CancellationToken);
		var updateBody = await ErtisAuthInstance.ReadJsonAsync(updateResponse);
		
		// Identical update: the route id identified the mail hook
		Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
		Assert.Equal("IdenticalDocumentError", updateBody.GetProperty("errorCode").GetString());
	}
	
	[Fact]
	public async Task UserType_CreateAndUpdate_WithoutIdAndMembership()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var createResponse = await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/user-types", new { name = "Customer", properties = new { } }, TestContext.Current.CancellationToken);
		var created = await AssertSuccessAsync(createResponse);
		Assert.Equal("customer", created["slug"]!.GetValue<string>());
		
		var id = created["_id"]!.GetValue<string>();
		using var updateResponse = await adminClient.PutAsJsonAsync($"{this.MembershipUrl}/user-types/{id}", new { name = "Customer", description = "Customers", properties = new { } }, TestContext.Current.CancellationToken);
		var updated = await AssertSuccessAsync(updateResponse);
		Assert.Equal("Customers", updated["description"]!.GetValue<string>());
	}
	
	#endregion
}