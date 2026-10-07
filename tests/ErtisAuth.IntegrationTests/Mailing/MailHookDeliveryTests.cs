using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.Mailing;

/// <summary>
/// Custom mail hooks (not the predefined activation / reset password hooks) fired by real events and delivered to a
/// <see cref="FakeSmtpServer"/>: the event's document is available to the subject and body templates.
/// </summary>
public class MailHookDeliveryTests : IClassFixture<MailingErtisAuthInstance>
{
	#region Fields
	
	private readonly MailingErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public MailHookDeliveryTests(MailingErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	private async Task CreateMailHookAsync(string recipient, string eventType = "UserCreated", string status = "active")
	{
		var mailHooks = await this.AdminResourceClientAsync("mailhooks");
		await mailHooks.CreateAsync(new
		{
			name = $"Hook {Guid.NewGuid():N}",
			@event = eventType,
			status,
			mailProvider = MailingErtisAuthInstance.MailProviderSlug,
			mailSubject = "New user: {{document.firstname}}",
			mailTemplate = "<p>{{document.firstname}} {{document.lastname}} ({{document.email_address}}) joined.</p>",
			fromName = "ErtisAuth",
			fromAddress = "no-reply@example.com",
			sendToUtilizer = false,
			recipients = new[] { new { displayName = "Operations", emailAddress = recipient } }
		});
	}
	
	private async Task<string> CreateUserAsync(string lastname)
	{
		var username = $"user{Guid.NewGuid():N}";
		var users = await this.AdminResourceClientAsync("users");
		await users.CreateAsync(new
		{
			username,
			firstname = "Jane",
			lastname,
			email_address = $"{username}@example.com",
			password = "Mail-P@ssw0rd!",
			role = "admin",
			user_type = "user"
		});
		
		return $"{username}@example.com";
	}
	
	#endregion
	
	#region Tests
	
	/// <summary>
	/// Regression guard: the placeholders on the event document were sent unresolved ("{{document.firstname}}").
	/// </summary>
	[Fact]
	public async Task CustomMailHook_ResolvesTheEventDocumentAndEncodesItsValues()
	{
		var recipient = $"ops-{Guid.NewGuid():N}@example.com";
		await this.CreateMailHookAsync(recipient);
		
		var emailAddress = await this.CreateUserAsync("<b>Doe</b>");
		
		var mail = await this._instance.Smtp.WaitForMessageAsync(recipient);
		Assert.Equal("New user: Jane", mail.Subject);
		Assert.Equal($"<p>Jane &lt;b&gt;Doe&lt;/b&gt; ({emailAddress}) joined.</p>", mail.HtmlBody?.Trim());
	}
	
	[Fact]
	public async Task PassiveOrOtherEventMailHooks_AreNotSent()
	{
		var passiveRecipient = $"passive-{Guid.NewGuid():N}@example.com";
		var otherEventRecipient = $"deleted-{Guid.NewGuid():N}@example.com";
		var activeRecipient = $"active-{Guid.NewGuid():N}@example.com";
		await this.CreateMailHookAsync(passiveRecipient, status: "passive");
		await this.CreateMailHookAsync(otherEventRecipient, eventType: "UserDeleted");
		await this.CreateMailHookAsync(activeRecipient);
		
		await this.CreateUserAsync("Doe");
		
		await this._instance.Smtp.WaitForMessageAsync(activeRecipient);
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Empty(this._instance.Smtp.MessagesTo(passiveRecipient));
		Assert.Empty(this._instance.Smtp.MessagesTo(otherEventRecipient));
	}
	
	#endregion
}