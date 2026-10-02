using ErtisAuth.Abstractions.Services;

namespace ErtisAuth.WebAPI.BackgroundServices;

/// <summary>
/// Processes the items of a background queue with a bounded concurrency.
/// On shutdown the queue is closed and drained within the host's shutdown timeout; the items still being processed when the timeout is over are cancelled.
/// </summary>
public abstract class BaseQueueBackgroundService<T> : BackgroundService
{
	#region Services
	
	private readonly IBackgroundQueue<T> _queue;
	private readonly ILogger _logger;
	
	#endregion
	
	#region Properties
	
	protected abstract int MaxConcurrency { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="queue"></param>
	/// <param name="logger"></param>
	protected BaseQueueBackgroundService(IBackgroundQueue<T> queue, ILogger logger)
	{
		this._queue = queue;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	protected abstract Task ProcessAsync(T item, CancellationToken cancellationToken);
	
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// The queue is read until it is completed and drained (not until stopping), so the queued items are still processed on shutdown;
		// stoppingToken cancels only the processing, when the shutdown timeout is over
		return Parallel.ForEachAsync(
			this._queue.ReadAllAsync(CancellationToken.None),
			new ParallelOptions { MaxDegreeOfParallelism = this.MaxConcurrency },
			async (item, _) =>
			{
				try
				{
					await this.ProcessAsync(item, stoppingToken);
				}
				catch (Exception ex)
				{
					// A failed item must not stop the worker
					this._logger.LogError(ex, "A queued {ItemType} could not be processed", typeof(T).Name);
				}
			});
	}
	
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		this._queue.Complete();
		
		try
		{
			if (this.ExecuteTask != null)
			{
				await this.ExecuteTask.WaitAsync(cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			this._logger.LogWarning("The {ItemType} queue could not be drained within the shutdown timeout, {Count} items are not processed", typeof(T).Name, this._queue.Count);
		}
		
		await base.StopAsync(cancellationToken);
	}
	
	#endregion
}
