using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.WebAPI.Tests.BackgroundServices;

/// <summary>
/// The webhook worker executes the queued webhook calls, up to eight at once.
/// The queue worker behaviors shared with the mail worker (failures, draining, shutdown timeout) are covered by MailHookBackgroundServiceTests.
/// </summary>
public class WebhookBackgroundServiceTests
{
	#region Fields
	
	private readonly BackgroundQueue<WebhookCall> _webhookQueue = new();
	
	private readonly IWebhookService _webhookService = Substitute.For<IWebhookService>();
	
	private int _executedCount;
	
	#endregion
	
	#region Helpers
	
	private WebhookBackgroundService CreateWorker()
	{
		return new WebhookBackgroundService(this._webhookQueue, this._webhookService, NullLogger<WebhookBackgroundService>.Instance);
	}
	
	private void Enqueue(int count)
	{
		for (var i = 0; i < count; i++)
		{
			var webhook = new Webhook { Name = $"webhook-{i}", Event = "UserCreated", MembershipId = "membership" };
			Assert.True(this._webhookQueue.TryEnqueue(new WebhookCall(webhook, "user", "membership", null)));
		}
	}
	
	private async Task WaitUntilExecutedAsync(int count)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (Volatile.Read(ref this._executedCount) < count && DateTime.UtcNow < deadline)
		{
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
		
		Assert.Equal(count, Volatile.Read(ref this._executedCount));
	}
	
	#endregion
	
	#region Methods
	
	[Fact]
	public async Task Worker_ExecutesTheQueuedCallsUpToEightAtOnce()
	{
		var concurrent = 0;
		var maxConcurrent = 0;
		this._webhookService
			.ExecuteWebhookAsync(Arg.Any<WebhookCall>(), Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				var current = Interlocked.Increment(ref concurrent);
				int observed;
				while ((observed = Volatile.Read(ref maxConcurrent)) < current && Interlocked.CompareExchange(ref maxConcurrent, current, observed) != observed)
				{
				}
				
				await Task.Delay(50, callInfo.ArgAt<CancellationToken>(1));
				Interlocked.Decrement(ref concurrent);
				Interlocked.Increment(ref this._executedCount);
			});
		
		var worker = this.CreateWorker();
		this.Enqueue(20);
		await worker.StartAsync(TestContext.Current.CancellationToken);
		
		await this.WaitUntilExecutedAsync(20);
		await worker.StopAsync(TestContext.Current.CancellationToken);
		
		Assert.Equal(8, maxConcurrent);
	}
	
	#endregion
}
