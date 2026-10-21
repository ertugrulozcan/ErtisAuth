using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using ErtisAuth.Extensions.Mailing.SmtpServer;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// The mail service sends with the membership's mail provider: the direct sending throws the failures, the queued sending
/// reports them to the completion callback. Both raise the mail events. The delivery modes are covered by MailhookServiceTests.
/// </summary>
public class MailServiceTests
{
	#region Constants
	
	private const string MembershipId = "5f8a1b2c3d4e5f6a7b8c9d00";
	
	#endregion
	
	#region Fields
	
	private readonly IMembershipService _membershipService = Substitute.For<IMembershipService>();
	
	private readonly IRawMailService _smtpService = Substitute.For<IRawMailService>();
	
	private readonly BackgroundQueue<QueuedMail> _mailQueue = new();
	
	private readonly Membership _membership;
	
	private readonly List<MailEventArgs> _sentEvents = [];
	
	private readonly List<MailEventArgs> _failedEvents = [];
	
	#endregion
	
	#region Constructors
	
	public MailServiceTests()
	{
		this._smtpService.GetProviderType().Returns(MailProviderType.SmtpServer);
		
		this._membership = TestServiceFactory.CreateMembership();
		this._membership.Id = MembershipId;
		this._membership.MailProviders =
		[
			new SmtpServerProvider
			{
				Name = "smtp",
				Host = "smtp.example.com",
				Port = 587,
				Username = "mailer",
				Password = "secret"
			}
		];
		
		this._membershipService.GetAsync(MembershipId, Arg.Any<CancellationToken>()).Returns(this._membership);
	}
	
	#endregion
	
	#region Helpers
	
	private MailService CreateService(IBackgroundQueue<QueuedMail>? mailQueue = null)
	{
		var service = new MailService(
			this._membershipService,
			[this._smtpService],
			mailQueue ?? this._mailQueue,
			NullLogger<MailService>.Instance);
		
		service.OnMailSent += (_, eventArgs) => this._sentEvents.Add(eventArgs);
		service.OnMailFailed += (_, eventArgs) => this._failedEvents.Add(eventArgs);
		
		return service;
	}
	
	private static MailMessage CreateMail(string mailProvider = "smtp", string membershipId = MembershipId)
	{
		return new MailMessage
		{
			MembershipId = membershipId,
			MailProvider = mailProvider,
			FromName = "ErtisAuth",
			FromAddress = "no-reply@example.com",
			Recipients = [new Recipient { DisplayName = "John Doe", EmailAddress = "john.doe@example.com" }],
			Subject = "Welcome",
			HtmlBody = "<p>Hello</p>"
		};
	}
	
	private void SmtpFails()
	{
		this._smtpService
			.SendMailAsync(Arg.Any<IMailProvider>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<Recipient>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("SMTP unavailable"));
	}
	
	#endregion
	
	#region Direct Sending
	
	[Fact]
	public async Task SendMailAsync_SendsWithTheMembershipProviderAndRaisesOnMailSent()
	{
		var mail = CreateMail();
		
		await this.CreateService().SendMailAsync(mail, TestContext.Current.CancellationToken);
		
		await this._smtpService.Received(1).SendMailAsync(this._membership.MailProviders!.Single(), "ErtisAuth", "no-reply@example.com", mail.Recipients, "Welcome", "<p>Hello</p>", Arg.Any<CancellationToken>());
		Assert.Same(mail, Assert.Single(this._sentEvents).Mail);
		Assert.Empty(this._failedEvents);
	}
	
	[Fact]
	public async Task SendMailAsync_WhenTheProviderFails_ThrowsAndRaisesOnMailFailed()
	{
		this.SmtpFails();
		
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => this.CreateService().SendMailAsync(CreateMail(), TestContext.Current.CancellationToken));
		
		Assert.Same(exception, Assert.Single(this._failedEvents).Error);
		Assert.Empty(this._sentEvents);
	}
	
	[Theory]
	[InlineData("unknown-provider", MembershipId)]
	[InlineData("smtp", "unknown-membership")]
	public async Task SendMailAsync_WithUnknownProviderOrMembership_ThrowsAndRaisesOnMailFailed(string mailProvider, string membershipId)
	{
		await Assert.ThrowsAnyAsync<Exception>(() => this.CreateService().SendMailAsync(CreateMail(mailProvider, membershipId), TestContext.Current.CancellationToken));
		
		Assert.Single(this._failedEvents);
		await this._smtpService.DidNotReceiveWithAnyArgs().SendMailAsync(null!, null!, null!, null!, null!, null!, CancellationToken.None);
	}
	
	[Fact]
	public async Task SendMailAsync_WhenAnEventSubscriberFails_DoesNotFailTheSending()
	{
		var service = this.CreateService();
		service.OnMailSent += (_, _) => throw new InvalidOperationException("Subscriber failed");
		
		await service.SendMailAsync(CreateMail(), TestContext.Current.CancellationToken);
		
		Assert.Single(this._sentEvents);
	}
	
	#endregion
	
	#region Queued Sending
	
	[Fact]
	public async Task SendQueuedMailAsync_CallsTheCallbackWithoutError()
	{
		var service = this.CreateService();
		var completions = new List<Exception?>();
		
		Assert.True(service.QueueMail(CreateMail(), error =>
		{
			completions.Add(error);
			return Task.CompletedTask;
		}));
		
		await service.SendQueuedMailAsync(await this.DequeueAsync(), TestContext.Current.CancellationToken);
		
		Assert.Null(Assert.Single(completions));
		Assert.Single(this._sentEvents);
	}
	
	[Fact]
	public async Task SendQueuedMailAsync_WhenTheProviderFails_CallsTheCallbackWithTheErrorAndDoesNotThrow()
	{
		this.SmtpFails();
		var service = this.CreateService();
		var completions = new List<Exception?>();
		
		service.QueueMail(CreateMail(), error =>
		{
			completions.Add(error);
			return Task.CompletedTask;
		});
		
		await service.SendQueuedMailAsync(await this.DequeueAsync(), TestContext.Current.CancellationToken);
		
		Assert.IsType<InvalidOperationException>(Assert.Single(completions));
		Assert.Single(this._failedEvents);
	}
	
	[Fact]
	public async Task SendQueuedMailAsync_WhenTheCallbackFails_DoesNotThrow()
	{
		var service = this.CreateService();
		service.QueueMail(CreateMail(), _ => throw new InvalidOperationException("Callback failed"));
		
		await service.SendQueuedMailAsync(await this.DequeueAsync(), TestContext.Current.CancellationToken);
		
		Assert.Single(this._sentEvents);
	}
	
	[Fact]
	public void QueueMail_WhenTheQueueRejectsTheMail_ReturnsFalseWithoutThrowing()
	{
		// e.g. a full queue, or a closed one on shutdown: the flow which queues the mail (e.g. a password reset) must not fail
		var rejectingQueue = Substitute.For<IBackgroundQueue<QueuedMail>>();
		rejectingQueue.TryEnqueue(Arg.Any<QueuedMail>()).Returns(false);
		
		Assert.False(this.CreateService(rejectingQueue).QueueMail(CreateMail()));
		
		rejectingQueue.Received(1).TryEnqueue(Arg.Any<QueuedMail>());
	}
	
	private async Task<QueuedMail> DequeueAsync()
	{
		this._mailQueue.Complete();
		await foreach (var queuedMail in this._mailQueue.ReadAllAsync(TestContext.Current.CancellationToken))
		{
			return queuedMail;
		}
		
		throw new InvalidOperationException("The queue is empty");
	}
	
	#endregion
}
