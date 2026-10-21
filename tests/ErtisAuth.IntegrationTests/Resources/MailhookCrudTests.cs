using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Resources;

public class MailhookCrudTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public MailhookCrudTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> CreateResourceClientAsync() =>
		new(await this._instance.CreateAdminClientAsync(), $"/memberships/{this._instance.MembershipId}/mailhooks");
	
	// Passive: the round trip must not send mails
	private static object MailhookBody(string name, string subject, string status = "passive") => new
	{
		name,
		description = "Welcome mail",
		@event = "UserCreated",
		status,
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
	public async Task Mailhook_CrudRoundTrip()
	{
		var mailhooks = await this.CreateResourceClientAsync();
		var name = $"Welcome {Guid.NewGuid():N}";
		
		var created = await mailhooks.CreateAsync(MailhookBody(name, "Welcome"));
		var id = created["_id"]!.GetValue<string>();
		var slug = created["slug"]!.GetValue<string>();
		Assert.Equal(this._instance.MembershipId, created["membership_id"]!.GetValue<string>());
		
		var fetched = await mailhooks.GetAsync(id);
		Assert.Equal(name, fetched["name"]!.GetValue<string>());
		Assert.Equal("<p>Welcome {{document.firstname}}</p>", fetched["mailTemplate"]!.GetValue<string>());
		Assert.Equal("support@example.com", fetched["recipients"]![0]!["emailAddress"]!.GetValue<string>());
		Assert.Equal("ErtisAuth", fetched["variables"]![0]!["value"]!.GetValue<string>());
		
		Assert.Contains(id, ResourceClient.IdsOf(await mailhooks.ListAsync()));
		Assert.Equal([id], ResourceClient.IdsOf(await mailhooks.QueryAsync(new { where = new { slug } })));
		
		var updated = await mailhooks.UpdateAsync(id, MailhookBody(name, "Welcome aboard"));
		Assert.Equal(id, updated["_id"]!.GetValue<string>());
		Assert.Equal("Welcome aboard", (await mailhooks.GetAsync(id))["mailSubject"]!.GetValue<string>());
		
		await mailhooks.DeleteAsync(id);
		await mailhooks.AssertNotFoundAsync(id, "MailhookNotFound");
	}
	
	[Fact]
	public async Task Mailhook_BulkDelete_DeletesAll()
	{
		var mailhooks = await this.CreateResourceClientAsync();
		var first = (await mailhooks.CreateAsync(MailhookBody($"First {Guid.NewGuid():N}", "First")))["_id"]!.GetValue<string>();
		var second = (await mailhooks.CreateAsync(MailhookBody($"Second {Guid.NewGuid():N}", "Second")))["_id"]!.GetValue<string>();
		
		using var response = await mailhooks.BulkDeleteAsync(first, second);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
		
		await mailhooks.AssertNotFoundAsync(first, "MailhookNotFound");
		await mailhooks.AssertNotFoundAsync(second, "MailhookNotFound");
	}
	
	/// <summary>
	/// Without a subject, a sender or any recipient no mail can be sent: rejected on create as well (was checked on update only).
	/// </summary>
	[Theory]
	[InlineData("mailSubject", "Mail subject is required")]
	[InlineData("fromName", "From name is required")]
	[InlineData("fromAddress", "From address is required")]
	[InlineData("recipients", "Recipients list is empty")]
	public async Task Mailhook_CreateWithoutARequiredField_IsRejected(string field, string error)
	{
		var mailhooks = await this.CreateResourceClientAsync();
		var body = JsonSerializer.SerializeToNode(MailhookBody($"Incomplete {Guid.NewGuid():N}", "Welcome", status: "active"))!.AsObject();
		body.Remove(field);
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var response = await adminClient.PostAsJsonAsync(mailhooks.Url, body, TestContext.Current.CancellationToken);
		
		var result = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		Assert.Equal("ValidationError", result!["errorCode"]!.GetValue<string>());
		Assert.Equal([error], result["data"]!.AsArray().Select(x => x!.GetValue<string>()));
	}
	
	/// <summary>
	/// A mail sent to the utilizer (e.g. activation, reset password) needs no other recipient.
	/// </summary>
	[Fact]
	public async Task Mailhook_SentToTheUtilizer_NeedsNoRecipients()
	{
		var mailhooks = await this.CreateResourceClientAsync();
		var body = JsonSerializer.SerializeToNode(MailhookBody($"To utilizer {Guid.NewGuid():N}", "Welcome"))!.AsObject();
		body.Remove("recipients");
		body["sendToUtilizer"] = true;
		
		var created = await mailhooks.CreateAsync(body);
		
		Assert.True(created["sendToUtilizer"]!.GetValue<bool>());
	}
	
	#endregion
}