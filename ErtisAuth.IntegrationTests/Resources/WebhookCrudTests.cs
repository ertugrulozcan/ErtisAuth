using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class WebhookCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public WebhookCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/webhooks");
	
	// Passive: the round trip must not fire requests
	private static object WebhookBody(string name, int tryCount) => new
	{
		name,
		description = "Notifies the CRM",
		@event = "UserCreated",
		status = "passive",
		request = new
		{
			method = "POST",
			url = "http://localhost:1/hooks/{{membership_id}}",
			headers = new Dictionary<string, string> { ["Authorization"] = "Bearer crm-token" },
			body = new { user = "{{document.username}}", nested = new { count = 1, active = true } }
		},
		try_count = tryCount
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Webhook_CrudRoundTrip()
	{
		var webhooks = await this.CreateResourceClientAsync();
		var name = $"Crm {Guid.NewGuid():N}";
		
		var created = await webhooks.CreateAsync(WebhookBody(name, 1));
		var id = created["_id"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		
		var fetched = await webhooks.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal("UserCreated", fetched["event"]!.GetValue<string>());
		var request = fetched["request"]!;
		Assert.Equal("POST", request["method"]!.GetValue<string>());
		Assert.Equal("http://localhost:1/hooks/{{membership_id}}", request["url"]!.GetValue<string>());
		Assert.Equal("Bearer crm-token", request["headers"]!["Authorization"]!.GetValue<string>());
		Assert.Equal("{{document.username}}", request["body"]!["user"]!.GetValue<string>());
		Assert.Equal(1, request["body"]!["nested"]!["count"]!.GetValue<int>());
		Assert.True(request["body"]!["nested"]!["active"]!.GetValue<bool>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await webhooks.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await webhooks.QueryAsync(new { where = new { name } })));
		
		var updated = await webhooks.UpdateAsync(id, WebhookBody(name, 3));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal(3, (await webhooks.GetAsync(id))["try_count"]!.GetValue<int>());
		
		await webhooks.DeleteAsync(id);
		await webhooks.AssertNotFoundAsync(id, "WebhookNotFound");
	}
	
	[Fact]
	public async Task Webhook_BulkDelete_DeletesAll()
	{
		var webhooks = await this.CreateResourceClientAsync();
		var first = (await webhooks.CreateAsync(WebhookBody($"First {Guid.NewGuid():N}", 1)))["_id"]!.GetValue<string>();
		var second = (await webhooks.CreateAsync(WebhookBody($"Second {Guid.NewGuid():N}", 1)))["_id"]!.GetValue<string>();
		
		using var response = await webhooks.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await webhooks.AssertNotFoundAsync(first, "WebhookNotFound");
		await webhooks.AssertNotFoundAsync(second, "WebhookNotFound");
	}
	
	#endregion
}