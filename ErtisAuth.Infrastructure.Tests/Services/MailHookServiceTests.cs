using System.Net;
using System.Text.Json;
using Ertis.Core.Collections;
using Ertis.Core.Exceptions;
using Ertis.Net.Rest;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
	
	private readonly IMailProvider _mailProvider = Substitute.For<IMailProvider>();
	
	private readonly List<MailHook> _mailHooks;
	
	private readonly List<SentMail> _sentMails = [];
	
	private readonly List<ErtisAuthEventType> _firedEvents = [];
	
	private string? _lastQuery;
	
	#endregion
	
	#region Constructors
	
	public MailHookServiceTests()
	{
		this._mailHooks = InMemoryRepository.Setup(this._repository, x => x.Id ??= ObjectId.GenerateNewId().ToString());
		
		this._mailProvider.Slug.Returns("smtp");
		this._mailProvider.DeliveryMode.Returns(DeliveryMode.Default);
		this._mailProvider
			.SendMailAsync(Arg.Any<ISystemRestHandler>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._sentMails)
				{
					this._sentMails.Add(new SentMail(callInfo.ArgAt<IEnumerable<Recipient>>(3).ToList(), callInfo.ArgAt<string>(4), callInfo.ArgAt<string>(5), null));
				}
				
				return Task.CompletedTask;
			});
		
		this._mailProvider
			.SendMailWithTemplateAsync(Arg.Any<ISystemRestHandler>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				lock (this._sentMails)
				{
					this._sentMails.Add(new SentMail(callInfo.ArgAt<IEnumerable<Recipient>>(3).ToList(), callInfo.ArgAt<string>(4), null, callInfo.ArgAt<IDictionary<string, string>>(6)));
				}
				
				return Task.CompletedTask;
			});
		
		var membership = TestServiceFactory.CreateMembership();
		membership.Id = MembershipId;
		membership.MailProviders = [this._mailProvider];
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(membership);
		
		this._userRepository.FindOneAsync(UserId, Arg.Any<CancellationToken>()).Returns((object) new User
		{
			Id = UserId,
			Username = "john.doe",
			EmailAddress = "john.doe@example.com",
			FirstName = "John",
			LastName = "Doe",
			Role = "user",
			MembershipId = MembershipId
		});
		
		// Event-triggered lookup: the stored hooks as the dynamic query returns them
		this._repository
			.QueryAsync((string) null!, null, null, null, (string?) null)
			.ReturnsForAnyArgs(callInfo =>
			{
				this._lastQuery = callInfo.ArgAt<string>(0);
				var items = this._mailHooks.Select(x => (dynamic) JsonSerializer.SerializeToElement(x)).ToArray();
				return (IPaginationCollection<dynamic>) new PaginationCollection<dynamic> { Count = items.Length, Items = items };
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
	
	private MailHookService CreateService()
	{
		return new MailHookService(this._membershipService, Substitute.For<ISystemRestHandler>(), this._eventService, this._repository, this._userRepository, NullLogger<MailHookService>.Instance);
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
		this.CreateService().SendHookMailAsync(mailHook, UserId, MembershipId, payload, TestContext.Current.CancellationToken);
		await this.WaitForOutcomeEventsAsync(1);
		return Assert.Single(this._sentMails);
	}
	
	/// <summary>
	/// Mails are sent in the background (fire and forget); wait for the outcome events.
	/// </summary>
	private async Task WaitForOutcomeEventsAsync(int count)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (this.OutcomeEvents().Count < count && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
		
		Assert.Equal(count, this.OutcomeEvents().Count);
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
		// Template providers (SendGrid, MailChimp) escape the variables themselves
		this._mailProvider.DeliveryMode.Returns(DeliveryMode.Template);
		var mailHook = CreateMailHook();
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
		this._mailProvider
			.SendMailAsync(Arg.Any<ISystemRestHandler>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("SMTP unavailable"));
		
		this.CreateService().SendHookMailAsync(CreateMailHook(), UserId, MembershipId, new { }, TestContext.Current.CancellationToken);
		await this.WaitForOutcomeEventsAsync(1);
		
		Assert.Equal(ErtisAuthEventType.MailhookMailFailed, Assert.Single(this.OutcomeEvents()));
	}
	
	[Theory]
	[InlineData("passive", "smtp")]
	[InlineData("active", "unknown-provider")]
	public async Task SendHookMailAsync_WithPassiveHookOrUnknownProvider_SendsNothing(string status, string mailProvider)
	{
		this.CreateService().SendHookMailAsync(CreateMailHook(status: status, mailProvider: mailProvider), UserId, MembershipId, new { }, TestContext.Current.CancellationToken);
		await Task.Delay(300, TestContext.Current.CancellationToken);
		
		Assert.Empty(this._sentMails);
		Assert.Empty(this.OutcomeEvents());
	}
	
	#endregion
	
	#region Triggering
	
	[Fact]
	public async Task OnEventFired_SendsTheCustomHooksOfTheEvent()
	{
		this._mailHooks.Add(CreateMailHook(name: "Welcome", template: "<p>Welcome {{document.firstname}}</p>"));
		this.CreateService();
		
		this.FireUserCreated(new { firstname = "John" });
		await this.WaitForOutcomeEventsAsync(1);
		
		Assert.Equal("<p>Welcome John</p>", Assert.Single(this._sentMails).HtmlBody);
		Assert.NotNull(this._lastQuery);
		Assert.Contains("\"active\"", this._lastQuery);
		Assert.Contains($"\"{MembershipId}\"", this._lastQuery);
		Assert.Contains("\"UserCreated\"", this._lastQuery);
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
		
		Assert.Empty(this._sentMails);
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
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => this.CreateService().CreateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, mailHook, TestContext.Current.CancellationToken));
		
		Assert.Contains(exception.Errors!, x => x.StartsWith(expectedErrorStart));
	}
	
	[Fact]
	public async Task CreateAsync_WithExistingName_ThrowsConflict()
	{
		var service = this.CreateService();
		await service.CreateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, CreateMailHook(), TestContext.Current.CancellationToken);
		
		var exception = await Assert.ThrowsAsync<ErtisAuth.Core.Exceptions.ErtisAuthException>(() => service.CreateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, CreateMailHook(), TestContext.Current.CancellationToken));
		
		Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
	}
	
	[Fact]
	public async Task UpdateAsync_WithoutRecipients_ThrowsValidationError()
	{
		var service = this.CreateService();
		var created = await service.CreateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, CreateMailHook(), TestContext.Current.CancellationToken);
		var update = CreateMailHook(sendToUtilizer: false, recipients: []);
		update.Id = created.Id;
		
		var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(Utilizer.GetSystemUtilizer(MembershipId), MembershipId, update, TestContext.Current.CancellationToken));
		
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