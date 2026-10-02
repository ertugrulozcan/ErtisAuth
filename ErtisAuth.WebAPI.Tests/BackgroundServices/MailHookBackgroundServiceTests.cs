using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.WebAPI.BackgroundServices;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.WebAPI.Tests.BackgroundServices;

/// <summary>
/// The worker sends the queued hook mails with a bounded concurrency, keeps working after a failed mail,
/// and drains the queue on shutdown until the shutdown timeout is over.
/// </summary>
public class MailHookBackgroundServiceTests
{
	#region Fields
	
	private readonly MailHookQueue _mailHookQueue = new();
	
	private readonly IMailHookService _mailHookService = Substitute.For<IMailHookService>();
	
	private readonly List<string> _sentMails = [];
	
	#endregion
	
	#region Helpers
	
	private MailHookBackgroundService CreateWorker()
	{
		return new MailHookBackgroundService(this._mailHookQueue, this._mailHookService, NullLogger<MailHookBackgroundService>.Instance);
	}
	
	private void Enqueue(params string[] names)
	{
		foreach (var name in names)
		{
			var mailHook = new MailHook { Name = name, MembershipId = "membership" };
			Assert.True(this._mailHookQueue.TryEnqueue(new HookMail(mailHook, "user", "membership", null)));
		}
	}
	
	/// <summary>
	/// Every send takes the given time, then it is recorded
	/// </summary>
	private void SendTakes(TimeSpan duration)
	{
		this._mailHookService
			.SendHookMailAsync(Arg.Any<HookMail>(), Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				await Task.Delay(duration, callInfo.ArgAt<CancellationToken>(1));
				this.Record(callInfo.ArgAt<HookMail>(0));
			});
	}
	
	private void Record(HookMail mail)
	{
		lock (this._sentMails)
		{
			this._sentMails.Add(mail.MailHook.Name);
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
		
		Assert.Equal(["a", "b", "c"], this._sentMails.Order());
	}
	
	[Fact]
	public async Task Worker_AfterAFailedMail_SendsTheNextMails()
	{
		this._mailHookService
			.SendHookMailAsync(Arg.Any<HookMail>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var mail = callInfo.ArgAt<HookMail>(0);
				if (mail.MailHook.Name == "failing")
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
		
		Assert.Equal("next", Assert.Single(this._sentMails));
	}
	
	[Fact]
	public async Task Worker_SendsAtMostFourMailsAtOnce()
	{
		var concurrent = 0;
		var maxConcurrent = 0;
		this._mailHookService
			.SendHookMailAsync(Arg.Any<HookMail>(), Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				var current = Interlocked.Increment(ref concurrent);
				InterlockedMax(ref maxConcurrent, current);
				await Task.Delay(50, callInfo.ArgAt<CancellationToken>(1));
				Interlocked.Decrement(ref concurrent);
				this.Record(callInfo.ArgAt<HookMail>(0));
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
		Assert.False(this._mailHookQueue.TryEnqueue(new HookMail(new MailHook { Name = "late", MembershipId = "membership" }, "user", "membership", null)));
	}
	
	[Fact]
	public async Task StopAsync_WhenTheShutdownTimeoutIsOver_CancelsTheSending()
	{
		this.SendTakes(Timeout.InfiniteTimeSpan);
		var worker = this.CreateWorker();
		this.Enqueue("never-ending");
		await worker.StartAsync(TestContext.Current.CancellationToken);
		await this.WaitUntilAsync(() => this._mailHookService.ReceivedCalls().Any());
		
		using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
		await worker.StopAsync(shutdownTimeout.Token);
		
		var stoppingToken = this._mailHookService.ReceivedCalls().Single().GetArguments()[1];
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
