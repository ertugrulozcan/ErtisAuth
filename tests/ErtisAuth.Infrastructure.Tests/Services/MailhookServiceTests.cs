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
/// Mail hooks: CRUD rules, which hooks an event triggers, recipients, the rendered mail and the outcome events.
/// The mails are sent through a real MailService and its queue. Payload values (e.g. a user's own name) are
/// user-controlled, so they must not inject markup into the HTML body.
/// </summary>
public class MailhookServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IEventService _eventService = Substitute.For<IEventService>();
	
	private readonly IMailhookRepository _repository = Substitute.For<IMailhookRepository>();
	
	private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
	
	// MailService finds the provider mail service by the provider type and sends through the non-generic interfaces
	private readonly IRawMailService _smtpService = Substitute.For<IRawMailService>();
	
	private readonly ITemplateMailService _mailChimpService = Substitute.For<ITemplateMailService>();
	
	private readonly BackgroundQueue<QueuedMail> _mailQueue = new();
	
	private IMailService _mailService = null!;
	
	private readonly Membership _membership;
	
	private readonly List<Mailhook> _mailhooks;
	
	private readonly List<SentMail> _sentMails = [];
	
	private readonly List<ErtisAuthEventType> _firedEvents = [];
	
	#endregion
	
	#region Constructors
	
	public MailhookServiceTests()
	{
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		this._mailhooks = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
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
	
	private MailhookService CreateService(IEnumerable<IProviderMailService>? providerMailServices = null, IMailService? mailService = null)
	{
		this._mailService = mailService ?? new MailService(
			this._membershipService,
			providerMailServices ?? [this._smtpService, this._mailChimpService],
			this._mailQueue,
			NullLogger<MailService>.Instance);
		
		return new MailhookService(
			this._membershipService,
			this._eventService,
			this._mailService,
			this._repository,
			this._userRepository,
			NullLogger<MailhookService>.Instance);
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
	
	private static Mailhook CreateMailhook(
		string name = "Welcome",
		string template = "<p>Hello</p>",
		string subject = "Welcome",
		bool sendToUtilizer = true,
		Recipient[]? recipients = null,
		string status = "active",
		string mailProvider = "smtp")
	{
		return new Mailhook
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
	
	private async Task<SentMail> SendAsync(Mailhook mailhook, object? payload)
	{
		await this.QueueAndSendAsync(this.CreateService(), mailhook, payload);
		return Assert.Single(this._sentMails);
	}
	
	private async Task QueueAndSendAsync(MailhookService service, Mailhook mailhook, object? payload)
	{
		await service.QueueHookMailAsync(mailhook, UserId, MembershipId, payload, TestContext.Current.CancellationToken);
		await this.SendQueuedMailsAsync(0);
	}
	
	/// <summary>
	/// The event handler queues the mails in the background: waits for the queued mails, then sends them like the mail worker does.
	/// </summary>
	private async Task SendQueuedMailsAsync(int expectedCount)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (this._mailQueue.Count < expectedCount && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
		
		this._mailQueue.Complete();
		await foreach (var mail in this._mailQueue.ReadAllAsync(TestContext.Current.CancellationToken))
		{
			await this._mailService.SendQueuedMailAsync(mail, TestContext.Current.CancellationToken);
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
	public async Task HookMail_HtmlEncodesThePayloadValuesInTheBody()
	{
		// Attack: a user's own name carrying a link into a mail sent from the system's address
		var user = new { firstname = "<a href=\"https://evil.example\">Verify your account</a>" };
		var template = "<p>Hi {{user.firstname}}, <a href=\"{{activationLink}}\">activate</a></p>";
		
		var mail = await this.SendAsync(CreateMailhook(template: template), new { user, activationLink = "https://app.example.com/activate?token=abc&m=1" });
		
		Assert.Equal("<p>Hi &lt;a href=&quot;https://evil.example&quot;&gt;Verify your account&lt;/a&gt;, <a href=\"https://app.example.com/activate?token=abc&amp;m=1\">activate</a></p>", mail.HtmlBody);
	}
	
	[Fact]
	public async Task HookMail_KeepsTheTemplateAndUnresolvedPlaceholders()
	{
		const string template = "<h1 class=\"title\">Hello &amp; welcome</h1><p>{{unknown.value}}</p>";
		
		var mail = await this.SendAsync(CreateMailhook(template: template), new { user = new { firstname = "John" } });
		
		Assert.Equal(template, mail.HtmlBody);
	}
	
	[Fact]
	public async Task HookMail_RemovesLineBreaksFromTheSubject()
	{
		var mail = await this.SendAsync(CreateMailhook(subject: "Welcome {{user.firstname}}"), new { user = new { firstname = "John\r\nBcc: attacker@example.com" } });
		
		Assert.DoesNotContain('\r', mail.Subject);
		Assert.DoesNotContain('\n', mail.Subject);
		Assert.StartsWith("Welcome John", mail.Subject);
	}
	
	[Fact]
	public async Task HookMail_WithTemplateDeliveryMode_SendsTheFormattedVariables()
	{
		// Template providers (MailChimp) escape the variables themselves
		this._membership.MailProviders = [new MailChimpProvider { Name = "mailchimp", ApiKey = "key" }];
		var mailhook = CreateMailhook(mailProvider: "mailchimp");
		mailhook.Variables = [new MailhookVariable { Key = "name", Value = "{{user.firstname}}" }, new MailhookVariable { Key = "empty", Value = null }];
		
		var mail = await this.SendAsync(mailhook, new { user = new { firstname = "<b>John</b>" } });
		
		Assert.Equal("<b>John</b>", mail.Arguments?["name"]);
		Assert.Equal(string.Empty, mail.Arguments?["empty"]);
	}
	
	#endregion
	
	#region Recipients & Outcome
	
	[Fact]
	public async Task HookMail_SendsToTheUtilizerAndTheFormattedRecipientsWithoutDuplicates()
	{
		var recipients = new[]
		{
			new Recipient { DisplayName = "Admin", EmailAddress = "admin@example.com" },
			new Recipient { DisplayName = "{{user.firstname}}", EmailAddress = "{{user.email_address}}" }
		};
		
		var mail = await this.SendAsync(CreateMailhook(recipients: recipients), new { user = new { firstname = "John", email_address = "john.doe@example.com" } });
		
		Assert.Equal(["john.doe@example.com", "admin@example.com"], mail.Recipients.Select(x => x.EmailAddress));
		Assert.Equal("John Doe", mail.Recipients[0].DisplayName);
		Assert.Equal(ErtisAuthEventType.MailhookMailSent, Assert.Single(this.OutcomeEvents()));
	}
	
	[Fact]
	public async Task HookMail_WhenTheProviderFails_FiresMailhookMailFailed()
	{
		this._smtpService
			.SendMailAsync(Arg.Any<IMailProvider>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("SMTP unavailable"));
		
		await this.QueueAndSendAsync(this.CreateService(), CreateMailhook(), new { });
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
	}
	
	[Fact]
	public async Task HookMail_WithoutMailServiceForTheProviderType_FiresMailhookMailFailed()
	{
		// Only the SMTP and MailChimp services are registered
		this._membership.MailProviders = [new SendGridProvider { Name = "sendgrid", ApiKey = "key" }];
		
		await this.QueueAndSendAsync(this.CreateService(), CreateMailhook(mailProvider: "sendgrid"), new { });
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
		Assert.Empty(this._sentMails);
	}
	
	[Fact]
	public async Task HookMail_WhenTheMailServiceDoesNotSupportTheDeliveryMode_FiresMailhookMailFailed()
	{
		// The MailChimp provider delivers with templates, but its registered service sends only raw mails
		var rawOnlyService = Substitute.For<IRawMailService>();
		rawOnlyService.GetProviderType().Returns(MailProviderType.MailChimp);
		this._membership.MailProviders = [new MailChimpProvider { Name = "mailchimp", ApiKey = "key" }];
		
		await this.QueueAndSendAsync(this.CreateService([rawOnlyService]), CreateMailhook(mailProvider: "mailchimp"), new { });
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
		await rawOnlyService.DidNotReceiveWithAnyArgs().SendMailAsync(null!, null!, null!, null!, null!, null!, CancellationToken.None);
	}
	
	[Fact]
	public async Task HookMail_WithUnknownProvider_FiresMailhookMailFailed()
	{
		await this.QueueAndSendAsync(this.CreateService(), CreateMailhook(mailProvider: "unknown-provider"), new { });
		
		Assert.Empty(this._sentMails);
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
	}
	
	#endregion
	
	#region Triggering
	
	[Fact]
	public async Task OnEventFired_SendsTheCustomHooksOfTheEvent()
	{
		this._mailhooks.Add(CreateMailhook(name: "Welcome", template: "<p>Welcome {{document.firstname}}</p>"));
		this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		await this.SendQueuedMailsAsync(1);
		
		Assert.Equal("<p>Welcome John</p>", Assert.Single(this._sentMails).HtmlBody);
	}
	
	[Fact]
	public async Task OnEventFired_SendsOnlyTheActiveHooksOfTheEventInTheMembership()
	{
		this._mailhooks.Add(CreateMailhook(name: "Matching", subject: "Matching"));
		
		var otherEvent = CreateMailhook(name: "Other Event", subject: "Other Event");
		otherEvent.Event = nameof(ErtisAuthEventType.UserDeleted);
		this._mailhooks.Add(otherEvent);
		
		var otherMembership = CreateMailhook(name: "Other Membership", subject: "Other Membership");
		otherMembership.MembershipId = "other-membership";
		this._mailhooks.Add(otherMembership);
		
		this._mailhooks.Add(CreateMailhook(name: "Passive", subject: "Passive", status: "passive"));
		this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		
		// Gives the other hooks the time to be (wrongly) queued
		await Task.Delay(300, TestContext.Current.CancellationToken);
		await this.SendQueuedMailsAsync(1);
		
		Assert.Equal("Matching", Assert.Single(this._sentMails).Subject);
	}
	
	/// <summary>
	/// Event documents are DynamicObjects (e.g. users): regression guard, their placeholders were not resolved.
	/// </summary>
	[Fact]
	public async Task OnEventFired_WithDynamicObjectDocument_ResolvesItsPlaceholders()
	{
		this._mailhooks.Add(CreateMailhook(name: "Welcome", template: "<p>Welcome {{document.firstname}}</p>", subject: "Hi {{document.firstname}}"));
		this.CreateService();
		
		this.FireUserCreated(DynamicObject.Parse("""{ "_id": "user-1", "firstname": "John" }"""));
		await this.SendQueuedMailsAsync(1);
		
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
		this._mailhooks.Add(CreateMailhook(name: name));
		this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		await Task.Delay(300, TestContext.Current.CancellationToken);
		
		Assert.Equal(0, this._mailQueue.Count);
	}
	
	#endregion
	
	#region Queuing
	
	[Fact]
	public async Task QueueHookMail_QueuesASnapshotOfThePayload()
	{
		// The mail is sent later: a change of the caller's objects after queuing must not reach the mail
		var service = this.CreateService();
		var user = new Dictionary<string, object?> { ["firstname"] = "John" };
		
		await service.QueueHookMailAsync(CreateMailhook(template: "<p>Hi {{user.firstname}}</p>"), UserId, MembershipId, new { user }, TestContext.Current.CancellationToken);
		user["firstname"] = "Changed";
		await this.SendQueuedMailsAsync(1);
		
		Assert.Equal("<p>Hi John</p>", Assert.Single(this._sentMails).HtmlBody);
	}
	
	[Fact]
	public async Task QueueHookMailAsync_WithPassiveHook_DoesNotQueueTheMail()
	{
		await this.CreateService().QueueHookMailAsync(CreateMailhook(status: "passive"), UserId, MembershipId, new { }, TestContext.Current.CancellationToken);
		
		Assert.Equal(0, this._mailQueue.Count);
	}
	
	[Fact]
	public async Task QueueHookMailAsync_WhenTheUtilizerHasNoEmailAddress_DoesNotQueueTheMail()
	{
		this._userRepository.FindOneAsync(UserId, Arg.Any<CancellationToken>()).Returns(new User
		{
			Id = UserId,
			Username = "john.doe",
			FirstName = "John",
			LastName = "Doe",
			Role = "user",
			MembershipId = MembershipId
		});
		
		await this.CreateService().QueueHookMailAsync(CreateMailhook(), UserId, MembershipId, new { }, TestContext.Current.CancellationToken);
		
		Assert.Equal(0, this._mailQueue.Count);
	}
	
	[Fact]
	public async Task QueueHookMailAsync_WhenTheMailServiceFails_DoesNotThrow()
	{
		// The flow which queues the mail (e.g. a password reset) must not fail
		var mailService = Substitute.For<IMailService>();
		mailService.QueueMail(Arg.Any<MailMessage>(), Arg.Any<Func<Exception?, Task>?>()).Throws(new InvalidOperationException("Unexpected"));
		
		await this.CreateService(mailService: mailService).QueueHookMailAsync(CreateMailhook(), UserId, MembershipId, new { }, TestContext.Current.CancellationToken);
		
		mailService.Received(1).QueueMail(Arg.Any<MailMessage>(), Arg.Any<Func<Exception?, Task>?>());
	}
	
	#endregion
	
	#region Create, Update & Lookup
	
	[Theory]
	[InlineData("enabled", "UserCreated", "Status should be 'active' or 'passive'")]
	[InlineData("active", "UnknownEvent", "Unknown event type.")]
	public async Task CreateAsync_WithInvalidHook_ThrowsValidationError(string status, string eventName, string expectedErrorStart)
	{
		var mailhook = CreateMailhook(status: status);
		mailhook.Event = eventName;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(mailhook, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Contains(exception.Errors!, x => x.StartsWith(expectedErrorStart));
	}
	
	[Fact]
	public async Task CreateAsync_WithExistingName_ThrowsConflict()
	{
		var service = this.CreateService();
		await service.CreateAsync(CreateMailhook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		
		var exception = await Assert.ThrowsAsync<Core.Exceptions.ErtisAuthException>(() => service.CreateAsync(CreateMailhook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutRecipients_ThrowsValidationError()
	{
		var service = this.CreateService();
		var created = await service.CreateAsync(CreateMailhook(), MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken);
		var update = CreateMailhook(sendToUtilizer: false, recipients: []);
		update.Id = created.Id;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(update, MembershipId, Utilizer.GetSystemUtilizer(MembershipId), TestContext.Current.CancellationToken));
		
		Assert.Contains("Recipients list is empty", exception.Errors!);
	}
	
	[Fact]
	public async Task GetUserActivationMailhookAsync_ReturnsOnlyTheActiveActivationHook()
	{
		this._mailhooks.Add(CreateMailhook(name: "User Activation", status: "passive"));
		var service = this.CreateService();
		
		Assert.Null(await service.GetUserActivationMailhookAsync(MembershipId, TestContext.Current.CancellationToken));
		
		this._mailhooks.Single().Status = "active";
		Assert.NotNull(await service.GetUserActivationMailhookAsync(MembershipId, TestContext.Current.CancellationToken));
	}
	
	#endregion
}
