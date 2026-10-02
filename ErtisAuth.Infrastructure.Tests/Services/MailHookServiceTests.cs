using Ertis.Schema.Dynamics;
using System.Net;
using Ertis.Core.Exceptions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using ErtisAuth.Extensions.Mailing.SmtpServer;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MailChimpProvider = ErtisAuth.Extensions.Mailing.MailChimp.MailChimpProvider;
using SendGridProvider = ErtisAuth.Extensions.Mailing.SendGrid.SendGridProvider;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Mail hooks: CRUD rules, which hooks an event triggers, recipients, and the rendered mail. Payload values
/// (e.g. a user's own name) are user-controlled, so they must not inject markup into the HTML body.
/// </summary>
public class MailHookServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly IMailHookRepository _repository = Substitute.For<IMailHookRepository>();
	
	private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
	
	// MailHookService finds the mail service by the provider type and sends through the non-generic interfaces
	private readonly IRawMailService _smtpService = Substitute.For<IRawMailService>();
	
	private readonly ITemplateMailService _mailChimpService = Substitute.For<ITemplateMailService>();
	
	private readonly BackgroundQueue<HookMail> _mailHookQueue = new();
	
	private readonly Membership _membership;
	
	private readonly List<MailHook> _mailHooks;
	
	private readonly List<SentMail> _sentMails = [];
	
	private readonly List<ErtisAuthEventType> _firedEvents = [];
	
	#endregion
	
	#region Constructors
	
	public MailHookServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._mailHooks = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		this._smtpService.GetProviderType().Returns(MailProviderType.SmtpServer);
		this._smtpService
			.SendMailAsync(Arg.Any<IMailProvider>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._sentMails)
				{
					this._sentMails.Add(new SentMail(callInfo.ArgAt<IEnumerable<Recipient>>(3).ToList(), callInfo.ArgAt<string>(4), callInfo.ArgAt<string>(5), null));
				}
				
				return Task.CompletedTask;
			});
		
		this._mailChimpService.GetProviderType().Returns(MailProviderType.MailChimp);
		this._mailChimpService
			.SendMailWithTemplateAsync(Arg.Any<IMailProvider>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._sentMails)
				{
					this._sentMails.Add(new SentMail(callInfo.ArgAt<IEnumerable<Recipient>>(3).ToList(), callInfo.ArgAt<string>(4), null, callInfo.ArgAt<IDictionary<string, string>>(6)));
				}
				
				return Task.CompletedTask;
			});
		
		this._membership = TestServiceFactory.CreateMembership();
		this._membership.Id = MembershipId;
		this._membership.MailProviders = [CreateSmtpProvider()];
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(this._membership);
		
		this._userRepository.FindOneAsync(UserId, Arg.Any<CancellationToken>()).Returns(new User
		{
			Id = UserId,
			Username = "john.doe",
			EmailAddress = "john.doe@example.com",
			FirstName = "John",
			LastName = "Doe",
			Role = "user",
			MembershipId = MembershipId
		});
		
		this._eventService
			.FireEventAsync(Arg.Any<ErtisAuthEventType>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._firedEvents)
				{
					this._firedEvents.Add(callInfo.ArgAt<ErtisAuthEventType>(0));
				}
				
				return new ErtisAuthEvent { EventType = callInfo.ArgAt<ErtisAuthEventType>(0), UtilizerId = UserId, MembershipId = MembershipId };
			});
	}
	
	#endregion
	
	#region Helpers
	
	private MailHookService CreateService(IEnumerable<IMailService>? mailServices = null, IBackgroundQueue<HookMail>? mailHookQueue = null)
	{
		return new MailHookService(
			this._membershipService,
			this._eventService,
			mailServices ?? [this._smtpService, this._mailChimpService],
			mailHookQueue ?? this._mailHookQueue,
			this._repository,
			this._userRepository,
			NullLogger<MailHookService>.Instance);
	}
	
	private static SmtpServerProvider CreateSmtpProvider()
	{
		return new SmtpServerProvider
		{
			Name = "smtp",
			Host = "smtp.example.com",
			Port = 587,
			Username = "mailer",
			Password = "secret"
		};
	}
	
	private static MailHook CreateMailHook(
		string name = "Welcome",
		string template = "<p>Hello</p>",
		string subject = "Welcome",
		bool sendToUtilizer = true,
		Recipient[]? recipients = null,
		string status = "active",
		string mailProvider = "smtp")
	{
		return new MailHook
		{
			Name = name,
			Event = nameof(ErtisAuthEventType.UserCreated),
			Status = status,
			MailSubject = subject,
			MailTemplate = template,
			FromName = "ErtisAuth",
			FromAddress = "no-reply@example.com",
			SendToUtilizer = sendToUtilizer,
			Recipients = recipients,
			MailProvider = mailProvider,
			MembershipId = MembershipId
		};
	}
	
	private async Task<SentMail> SendAsync(MailHook mailHook, object? payload)
	{
		await SendHookMailAsync(this.CreateService(), mailHook, payload);
		return Assert.Single(this._sentMails);
	}
	
	private static Task SendHookMailAsync(MailHookService service, MailHook mailHook, object? payload)
	{
		return service.SendHookMailAsync(new HookMail(mailHook, UserId, MembershipId, payload), TestContext.Current.CancellationToken);
	}
	
	/// <summary>
	/// The event handler queues the mails in the background: waits for the queued mails, then sends them like the mail hook worker does.
	/// </summary>
	private async Task SendQueuedMailsAsync(MailHookService service, int expectedCount)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (this._mailHookQueue.Count < expectedCount && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
		
		this._mailHookQueue.Complete();
		await foreach (var mail in this._mailHookQueue.ReadAllAsync(TestContext.Current.CancellationToken))
		{
			await service.SendHookMailAsync(mail, TestContext.Current.CancellationToken);
		}
	}
	
	private List<ErtisAuthEventType> OutcomeEvents()
	{
		lock (this._firedEvents)
		{
			return this._firedEvents.Where(x => x is ErtisAuthEventType.MailhookMailSent or ErtisAuthEventType.MailhookMailFailed).ToList();
		}
	}
	
	private void FireUserCreated(object document)
	{
		this._eventService.OnEventFired += Raise.Event<EventHandler<ErtisAuthEvent>>(this._eventService, new ErtisAuthEvent
		{
			EventType = ErtisAuthEventType.UserCreated,
			UtilizerId = UserId,
			MembershipId = MembershipId,
			Document = document
		});
	}
	
	private sealed record SentMail(List<Recipient> Recipients, string Subject, string? HtmlBody, IDictionary<string, string>? Arguments);
	
	#endregion
	
	#region Rendering
	
	[Fact]
	public async Task SendHookMailAsync_HtmlEncodesThePayloadValuesInTheBody()
	{
		// Attack: a user's own name carrying a link into a mail sent from the system's address
		var user = new { firstname = "<a href=\"https://evil.example\">Verify your account</a>" };
		var template = "<p>Hi {{user.firstname}}, <a href=\"{{activationLink}}\">activate</a></p>";
		
		var mail = await this.SendAsync(CreateMailHook(template: template), new { user, activationLink = "https://app.example.com/activate?token=abc&m=1" });
		
		Assert.Equal("<p>Hi &lt;a href=&quot;https://evil.example&quot;&gt;Verify your account&lt;/a&gt;, <a href=\"https://app.example.com/activate?token=abc&amp;m=1\">activate</a></p>", mail.HtmlBody);
	}
	
	[Fact]
	public async Task SendHookMailAsync_KeepsTheTemplateAndUnresolvedPlaceholders()
	{
		const string template = "<h1 class=\"title\">Hello &amp; welcome</h1><p>{{unknown.value}}</p>";
		
		var mail = await this.SendAsync(CreateMailHook(template: template), new { user = new { firstname = "John" } });
		
		Assert.Equal(template, mail.HtmlBody);
	}
	
	[Fact]
	public async Task SendHookMailAsync_RemovesLineBreaksFromTheSubject()
	{
		var mail = await this.SendAsync(CreateMailHook(subject: "Welcome {{user.firstname}}"), new { user = new { firstname = "John\r\nBcc: attacker@example.com" } });
		
		Assert.DoesNotContain('\r', mail.Subject);
		Assert.DoesNotContain('\n', mail.Subject);
		Assert.StartsWith("Welcome John", mail.Subject);
	}
	
	[Fact]
	public async Task SendHookMailAsync_WithTemplateDeliveryMode_SendsTheFormattedVariables()
	{
		// Template providers (MailChimp) escape the variables themselves
		this._membership.MailProviders = [new MailChimpProvider { Name = "mailchimp", ApiKey = "key" }];
		var mailHook = CreateMailHook(mailProvider: "mailchimp");
		mailHook.Variables = [new MailHookVariable { Key = "name", Value = "{{user.firstname}}" }, new MailHookVariable { Key = "empty", Value = null }];
		
		var mail = await this.SendAsync(mailHook, new { user = new { firstname = "<b>John</b>" } });
		
		Assert.Equal("<b>John</b>", mail.Arguments?["name"]);
		Assert.Equal(string.Empty, mail.Arguments?["empty"]);
	}
	
	#endregion
	
	#region Recipients & Outcome
	
	[Fact]
	public async Task SendHookMailAsync_SendsToTheUtilizerAndTheFormattedRecipientsWithoutDuplicates()
	{
		var recipients = new[]
		{
			new Recipient { DisplayName = "Admin", EmailAddress = "admin@example.com" },
			new Recipient { DisplayName = "{{user.firstname}}", EmailAddress = "{{user.email_address}}" }
		};
		
		var mail = await this.SendAsync(CreateMailHook(recipients: recipients), new { user = new { firstname = "John", email_address = "john.doe@example.com" } });
		
		Assert.Equal(["john.doe@example.com", "admin@example.com"], mail.Recipients.Select(x => x.EmailAddress));
		Assert.Equal("John Doe", mail.Recipients[0].DisplayName);
		Assert.Equal(ErtisAuthEventType.MailhookMailSent, Assert.Single(this.OutcomeEvents()));
	}
	
	[Fact]
	public async Task SendHookMailAsync_WhenTheProviderFails_FiresMailhookMailFailed()
	{
		this._smtpService
			.SendMailAsync(Arg.Any<IMailProvider>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("SMTP unavailable"));
		
		await SendHookMailAsync(this.CreateService(), CreateMailHook(), new { });
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
	}
	
	[Fact]
	public async Task SendHookMailAsync_WithoutMailServiceForTheProviderType_FiresMailhookMailFailed()
	{
		// Only the SMTP and MailChimp services are registered
		this._membership.MailProviders = [new SendGridProvider { Name = "sendgrid", ApiKey = "key" }];
		
		await SendHookMailAsync(this.CreateService(), CreateMailHook(mailProvider: "sendgrid"), new { });
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
		Assert.Empty(this._sentMails);
	}
	
	[Fact]
	public async Task SendHookMailAsync_WhenTheMailServiceDoesNotSupportTheDeliveryMode_FiresMailhookMailFailed()
	{
		// The MailChimp provider delivers with templates, but its registered service sends only raw mails
		var rawOnlyService = Substitute.For<IRawMailService>();
		rawOnlyService.GetProviderType().Returns(MailProviderType.MailChimp);
		this._membership.MailProviders = [new MailChimpProvider { Name = "mailchimp", ApiKey = "key" }];
		
		await SendHookMailAsync(this.CreateService([rawOnlyService]), CreateMailHook(mailProvider: "mailchimp"), new { });
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
		await rawOnlyService.DidNotReceiveWithAnyArgs().SendMailAsync(null!, null!, null!, null!, null!, null!, CancellationToken.None);
	}
	
	[Theory]
	[InlineData("passive", "smtp")]
	[InlineData("active", "unknown-provider")]
	public async Task SendHookMailAsync_WithPassiveHookOrUnknownProvider_SendsNothing(string status, string mailProvider)
	{
		await SendHookMailAsync(this.CreateService(), CreateMailHook(status: status, mailProvider: mailProvider), new { });
		
		Assert.Empty(this._sentMails);
		Assert.Empty(this.OutcomeEvents());
	}
	
	#endregion
	
	#region Triggering
	
	[Fact]
	public async Task OnEventFired_SendsTheCustomHooksOfTheEvent()
	{
		this._mailHooks.Add(CreateMailHook(name: "Welcome", template: "<p>Welcome {{document.firstname}}</p>"));
		var service = this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		await this.SendQueuedMailsAsync(service, 1);
		
		Assert.Equal("<p>Welcome John</p>", Assert.Single(this._sentMails).HtmlBody);
	}
	
	[Fact]
	public async Task OnEventFired_SendsOnlyTheActiveHooksOfTheEventInTheMembership()
	{
		this._mailHooks.Add(CreateMailHook(name: "Matching", subject: "Matching"));
		
		var otherEvent = CreateMailHook(name: "Other Event", subject: "Other Event");
		otherEvent.Event = nameof(ErtisAuthEventType.UserDeleted);
		this._mailHooks.Add(otherEvent);
		
		var otherMembership = CreateMailHook(name: "Other Membership", subject: "Other Membership");
		otherMembership.MembershipId = "other-membership";
		this._mailHooks.Add(otherMembership);
		
		this._mailHooks.Add(CreateMailHook(name: "Passive", subject: "Passive", status: "passive"));
		var service = this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		
		// Gives the other hooks the time to be (wrongly) queued
		await Task.Delay(300, TestContext.Current.CancellationToken);
		await this.SendQueuedMailsAsync(service, 1);
		
		Assert.Equal("Matching", Assert.Single(this._sentMails).Subject);
	}
	
	/// <summary>
	/// Event documents are DynamicObjects (e.g. users): regression guard, their placeholders were not resolved.
	/// </summary>
	[Fact]
	public async Task OnEventFired_WithDynamicObjectDocument_ResolvesItsPlaceholders()
	{
		this._mailHooks.Add(CreateMailHook(name: "Welcome", template: "<p>Welcome {{document.firstname}}</p>", subject: "Hi {{document.firstname}}"));
		var service = this.CreateService();
		
		this.FireUserCreated(DynamicObject.Parse("""{ "_id": "user-1", "firstname": "John" }"""));
		await this.SendQueuedMailsAsync(service, 1);
		
		var mail = Assert.Single(this._sentMails);
		Assert.Equal("<p>Welcome John</p>", mail.HtmlBody);
		Assert.Equal("Hi John", mail.Subject);
	}
	
	[Theory]
	[InlineData("User Activation")]
	[InlineData("Reset Password")]
	public async Task OnEventFired_DoesNotSendThePredefinedHooks(string name)
	{
		// The activation and reset password hooks are sent by their own flows, with their links
		this._mailHooks.Add(CreateMailHook(name: name));
		this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		await Task.Delay(300, TestContext.Current.CancellationToken);
		
		Assert.Equal(0, this._mailHookQueue.Count);
	}
	
	#endregion
	
	#region Queuing
	
	[Fact]
	public async Task QueueHookMail_QueuesASnapshotOfThePayload()
	{
		// The mail is sent later: a change of the caller's objects after queuing must not reach the mail
		var service = this.CreateService();
		var user = new Dictionary<string, object?> { ["firstname"] = "John" };
		
		service.QueueHookMail(CreateMailHook(template: "<p>Hi {{user.firstname}}</p>"), UserId, MembershipId, new { user });
		user["firstname"] = "Changed";
		await this.SendQueuedMailsAsync(service, 1);
		
		Assert.Equal("<p>Hi John</p>", Assert.Single(this._sentMails).HtmlBody);
	}
	
	[Fact]
	public void QueueHookMail_WhenTheQueueRejectsTheMail_DoesNotThrow()
	{
		// e.g. a full queue, or a closed one on shutdown: the flow which queues the mail (e.g. a password reset) must not fail
		var rejectingQueue = Substitute.For<IBackgroundQueue<HookMail>>();
		rejectingQueue.TryEnqueue(Arg.Any<HookMail>()).Returns(false);
		
		this.CreateService(mailHookQueue: rejectingQueue).QueueHookMail(CreateMailHook(), UserId, MembershipId, new { });
		
		rejectingQueue.Received(1).TryEnqueue(Arg.Any<HookMail>());
	}
	
	#endregion
	
	#region Create, Update & Lookup
	
	[Theory]
	[InlineData("enabled", "UserCreated", "Status should be 'active' or 'passive'")]
	[InlineData("active", "UnknownEvent", "Unknown event type.")]
	public async Task CreateAsync_WithInvalidHook_ThrowsValidationError(string status, string eventName, string expectedErrorStart)
	{
		var mailHook = CreateMailHook(status: status);
		mailHook.Event = eventName;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(mailHook, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Contains(exception.Errors!, x => x.StartsWith(expectedErrorStart));
	}
	
	[Fact]
	public async Task CreateAsync_WithExistingName_ThrowsConflict()
	{
		var service = this.CreateService();
		await service.CreateAsync(CreateMailHook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		var exception = await Assert.ThrowsAsync<Core.Exceptions.ErtisAuthException>(() => service.CreateAsync(CreateMailHook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutRecipients_ThrowsValidationError()
	{
		var service = this.CreateService();
		var created = await service.CreateAsync(CreateMailHook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		var update = CreateMailHook(sendToUtilizer: false, recipients: []);
		update.Id = created.Id;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Contains("Recipients list is empty", exception.Errors!);
	}
	
	[Fact]
	public async Task GetUserActivationMailHookAsync_ReturnsOnlyTheActiveActivationHook()
	{
		this._mailHooks.Add(CreateMailHook(name: "User Activation", status: "passive"));
		var service = this.CreateService();
		
		Assert.Null(await service.GetUserActivationMailHookAsync(MembershipId, TestContext.Current.CancellationToken));
		
		this._mailHooks.Single().Status = "active";
		Assert.NotNull(await service.GetUserActivationMailHookAsync(MembershipId, TestContext.Current.CancellationToken));
	}
	
	#endregion
}
