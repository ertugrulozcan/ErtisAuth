using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class MailHookCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public MailHookCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/mailhooks");
	
	// Passive: the round trip must not send mails
	private static object MailHookBody(string name, string subject) => new
	{
		name,
		description = "Welcome mail",
		@event = "UserCreated",
		status = "passive",
		mailProvider = "smtp",
		mailSubject = subject,
		mailTemplate = "<p>Welcome {{document.firstname}}</p>",
		fromName = "ErtisAuth",
		fromAddress = "no-reply@example.com",
		sendToUtilizer = false,
		recipients = new[] { new { displayName = "Support", emailAddress = "support@example.com" } },
		variables = new[] { new { key = "product", value = "ErtisAuth" } }
	};
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task MailHook_CrudRoundTrip()
	{
		var mailHooks = await this.CreateResourceClientAsync();
		var name = $"Welcome {Guid.NewGuid():N}";
		
		var created = await mailHooks.CreateAsync(MailHookBody(name, "Welcome"));
		var id = created["_id"]!.GetValue<string>();
		var slug = created["slug"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		
		var fetched = await mailHooks.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal("<p>Welcome {{document.firstname}}</p>", fetched["mailTemplate"]!.GetValue<string>());
		Assert.Equal("support@example.com", fetched["recipients"]![0]!["emailAddress"]!.GetValue<string>());
		Assert.Equal("ErtisAuth", fetched["variables"]![0]!["value"]!.GetValue<string>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await mailHooks.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await mailHooks.QueryAsync(new { where = new { slug } })));
		
		var updated = await mailHooks.UpdateAsync(id, MailHookBody(name, "Welcome aboard"));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal("Welcome aboard", (await mailHooks.GetAsync(id))["mailSubject"]!.GetValue<string>());
		
		await mailHooks.DeleteAsync(id);
		await mailHooks.AssertNotFoundAsync(id, "MailHookNotFound");
	}
	
	[Fact]
	public async Task MailHook_BulkDelete_DeletesAll()
	{
		var mailHooks = await this.CreateResourceClientAsync();
		var first = (await mailHooks.CreateAsync(MailHookBody($"First {Guid.NewGuid():N}", "First")))["_id"]!.GetValue<string>();
		var second = (await mailHooks.CreateAsync(MailHookBody($"Second {Guid.NewGuid():N}", "Second")))["_id"]!.GetValue<string>();
		
		using var response = await mailHooks.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await mailHooks.AssertNotFoundAsync(first, "MailHookNotFound");
		await mailHooks.AssertNotFoundAsync(second, "MailHookNotFound");
	}
	
	#endregion
}