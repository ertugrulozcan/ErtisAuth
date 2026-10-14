using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.WebAPI.Tests.BackgroundServices;

/// <summary>
/// The worker sends the queued mails with a bounded concurrency, keeps working after a failed mail,
/// and drains the queue on shutdown until the shutdown timeout is over.
/// </summary>
public class MailBackgroundServiceTests
{
	#region Fields
	
	private readonly BackgroundQueue<QueuedMail> _mailQueue = new();
	
	private readonly IMailService _mailService = Substitute.For<IMailService>();
	
	private readonly List<string> _sentMails = [];
	
	#endregion
	
	#region Helpers
	
	private static QueuedMail CreateQueuedMail(string subject)
	{
		var mail = new MailMessage
		{
			MembershipId = "membership",
			MailProvider = "smtp",
			FromName = "ErtisAuth",
			FromAddress = "no-reply@example.com",
			Recipients = [],
			Subject = subject
		};
		
		return new QueuedMail(mail, null);
	}
	
	private MailBackgroundService CreateWorker()
	{
		return new MailBackgroundService(this._mailQueue, this._mailService, NullLogger<MailBackgroundService>.Instance);
	}
	
	private void Enqueue(params string[] names)
	{
		foreach (var name in names)
		{
			Assert.True(this._mailQueue.TryEnqueue(CreateQueuedMail(name)));
		}
	}
	
	/// <summary>
	/// Every send takes the given time, then it is recorded
	/// </summary>
	private void SendTakes(TimeSpan duration)
	{
		this._mailService
			.SendQueuedMailAsync(Arg.Any<QueuedMail>(), Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				await Task.Delay(duration, callInfo.ArgAt<CancellationToken>(1));
				this.Record(callInfo.ArgAt<QueuedMail>(0));
			});
	}
	
	private void Record(QueuedMail mail)
	{
		lock (this._sentMails)
		{
			this._sentMails.Add(mail.Mail.Subject);
		}
	}
	
	private int SentCount()
	{
		lock (this._sentMails)
		{
			return this._sentMails.Count;
		}
	}
	
	private async Task WaitUntilAsync(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition() && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
		
		Assert.True(condition());
	}
	
	#endregion
	
	#region Methods
	
	[Fact]
	public async Task Worker_SendsTheQueuedMails()
	{
		this.SendTakes(TimeSpan.Zero);
		var worker = this.CreateWorker();
		await worker.StartAsync(TestContext.Current.CancellationToken);
		
		this.Enqueue("a", "b", "c");
		await this.WaitUntilAsync(() => this.SentCount() == 3);
		await worker.StopAsync(TestContext.Current.CancellationToken);
		
		// ReSharper disable once InconsistentlySynchronizedField
		Assert.Equal(["a", "b", "c"], this._sentMails.Order());
	}
	
	[Fact]
	public async Task Worker_AfterAFailedMail_SendsTheNextMails()
	{
		this._mailService
			.SendQueuedMailAsync(Arg.Any<QueuedMail>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var mail = callInfo.ArgAt<QueuedMail>(0);
				if (mail.Mail.Subject == "failing")
				{
					throw new InvalidOperationException("Unexpected");
				}
				
				this.Record(mail);
				return Task.CompletedTask;
			});
		
		var worker = this.CreateWorker();
		await worker.StartAsync(TestContext.Current.CancellationToken);
		
		this.Enqueue("failing", "next");
		await this.WaitUntilAsync(() => this.SentCount() == 1);
		await worker.StopAsync(TestContext.Current.CancellationToken);
		
		// ReSharper disable once InconsistentlySynchronizedField
		Assert.Equal("next", Assert.Single(this._sentMails));
	}
	
	[Fact]
	public async Task Worker_SendsAtMostFourMailsAtOnce()
	{
		var concurrent = 0;
		var maxConcurrent = 0;
		this._mailService
			.SendQueuedMailAsync(Arg.Any<QueuedMail>(), Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				var current = Interlocked.Increment(ref concurrent);
				InterlockedMax(ref maxConcurrent, current);
				await Task.Delay(50, callInfo.ArgAt<CancellationToken>(1));
				Interlocked.Decrement(ref concurrent);
				this.Record(callInfo.ArgAt<QueuedMail>(0));
			});
		
		var worker = this.CreateWorker();
		this.Enqueue(Enumerable.Range(0, 12).Select(x => x.ToString()).ToArray());
		await worker.StartAsync(TestContext.Current.CancellationToken);
		
		await this.WaitUntilAsync(() => this.SentCount() == 12);
		await worker.StopAsync(TestContext.Current.CancellationToken);
		
		Assert.Equal(4, maxConcurrent);
	}
	
	[Fact]
	public async Task StopAsync_SendsTheMailsQueuedBeforeTheStop()
	{
		this.SendTakes(TimeSpan.FromMilliseconds(100));
		var worker = this.CreateWorker();
		this.Enqueue("a", "b", "c", "d", "e", "f");
		await worker.StartAsync(TestContext.Current.CancellationToken);
		
		await worker.StopAsync(TestContext.Current.CancellationToken);
		
		Assert.Equal(6, this.SentCount());
		Assert.False(this._mailQueue.TryEnqueue(CreateQueuedMail("late")));
	}
	
	[Fact]
	public async Task StopAsync_WhenTheShutdownTimeoutIsOver_CancelsTheSending()
	{
		this.SendTakes(Timeout.InfiniteTimeSpan);
		var worker = this.CreateWorker();
		this.Enqueue("never-ending");
		await worker.StartAsync(TestContext.Current.CancellationToken);
		await this.WaitUntilAsync(() => this._mailService.ReceivedCalls().Any());
		
		using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
		await worker.StopAsync(shutdownTimeout.Token);
		
		var stoppingToken = this._mailService.ReceivedCalls().Single().GetArguments()[1];
		Assert.True(stoppingToken is CancellationToken { IsCancellationRequested: true });
		Assert.Equal(0, this.SentCount());
	}
	
	private static void InterlockedMax(ref int target, int value)
	{
		int current;
		do
		{
			current = target;
			if (value <= current)
			{
				return;
			}
		}
		while (Interlocked.CompareExchange(ref target, value, current) != current);
	}
	
	#endregion
}
