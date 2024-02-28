using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Extensions.Hosting;

public sealed class Worker : IHostedService, IDisposable
{
	#region Services

	private readonly IBackgroundTaskQueue _taskQueue;
	private readonly ILogger<Worker> _logger;

	#endregion

	#region Fields

	private Task? _executingTask;
	private CancellationTokenSource? _stoppingCts;

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="taskQueue"></param>
	/// <param name="logger"></param>
	public Worker(IBackgroundTaskQueue taskQueue, ILogger<Worker> logger)
	{
		this._taskQueue = taskQueue;
		this._logger = logger;
	}

	#endregion

	#region Methods

	private async Task ExecuteBackgroundTaskQueueAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var workItem = await this._taskQueue.DequeueAsync(stoppingToken);

			try
			{
				await workItem(stoppingToken);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Error occurred executing {WorkItem}", nameof(workItem));
			}
		}
	}

	private async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await this.ExecuteBackgroundTaskQueueAsync(stoppingToken);
		}
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		this._logger.LogInformation("Queue processing worker has started");
		
		this._stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		this._executingTask = ExecuteAsync(_stoppingCts.Token);

		return _executingTask.IsCompleted ? this._executingTask : Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		if (this._executingTask == null)
		{
			return;
		}

		try
		{
			this._stoppingCts?.Cancel();
		}
		finally
		{
			// Wait until the task completes or the stop token triggers
			await Task.WhenAny(this._executingTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
		}
	}

	#endregion

	#region Disposing

	public void Dispose()
	{
		this._stoppingCts?.Cancel();
	}

	#endregion
}