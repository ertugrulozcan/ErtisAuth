using Microsoft.Extensions.Logging;

namespace ErtisAuth.Extensions.Hosting;

public abstract class BackgroundWorker<TIn> : IBackgroundWorker<TIn> where TIn : class
{
	#region Services

	private readonly IBackgroundTaskQueue _taskQueue;
	private readonly ILogger _logger;
	
	#endregion
	
	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="taskQueue"></param>
	/// <param name="logger"></param>
	protected BackgroundWorker(IBackgroundTaskQueue taskQueue, ILogger<BackgroundWorker<TIn>> logger)
	{
		this._taskQueue = taskQueue;
		this._logger = logger;
	}

	#endregion

	#region Events

	public event EventHandler<BackgroundTaskStartedEventArgs<TIn>>? OnStarted;
	public event EventHandler<BackgroundTaskFailedEventArgs<TIn>>? OnFailed;
	public event EventHandler<BackgroundTaskFinishedEventArgs<TIn>>? OnFinished;

	#endregion

	#region Abstract Methods

	protected abstract ValueTask ExecuteAsync(TIn? args = null, CancellationToken token = default);

	#endregion
	
	#region Methods

	public async ValueTask StartAsync(TIn? args = null)
	{
		var guid = Guid.NewGuid().ToString();
		await this._taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
		{
			try
			{
				await this.ExecuteAsync(args, cancellationToken);
					
				this._logger.LogInformation("Background task {Guid} finished", guid);
				this.OnFinished?.Invoke(this, new BackgroundTaskFinishedEventArgs<TIn>(args));
			}
			catch (Exception ex)
			{
				this._logger.LogError("Background task {Guid} failed", guid);
				this._logger.LogError("{Message}", ex.Message);
				this.OnFailed?.Invoke(this, new BackgroundTaskFailedEventArgs<TIn>(args, ex));
			}
		});

		this._logger.LogInformation("Background task {Guid} started", guid);
		this.OnStarted?.Invoke(this, new BackgroundTaskStartedEventArgs<TIn>(args));
	}

	#endregion
}

public abstract class BackgroundWorker<TIn, TOut> : IBackgroundWorker<TIn, TOut> where TIn : class
{
	#region Services

	private readonly IBackgroundTaskQueue _taskQueue;
	private readonly ILogger _logger;
	
	#endregion
	
	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="taskQueue"></param>
	/// <param name="logger"></param>
	protected BackgroundWorker(IBackgroundTaskQueue taskQueue, ILogger<BackgroundWorker<TIn, TOut>> logger)
	{
		this._taskQueue = taskQueue;
		this._logger = logger;
	}

	#endregion

	#region Events

	public event EventHandler<BackgroundTaskStartedEventArgs<TIn>>? OnStarted;
	public event EventHandler<BackgroundTaskFailedEventArgs<TIn>>? OnFailed;
	public event EventHandler<BackgroundTaskFinishedEventArgs<TIn, TOut>>? OnFinished;

	#endregion

	#region Abstract Methods

	protected abstract ValueTask<TOut> ExecuteAsync(TIn? args = null, CancellationToken token = default);

	#endregion
	
	#region Methods

	public async ValueTask StartAsync(TIn? args = null)
	{
		var guid = Guid.NewGuid().ToString();
		await this._taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
		{
			try
			{
				var result = await this.ExecuteAsync(args, cancellationToken);
					
				this._logger.LogInformation("Background task {Guid} finished", guid);
				this.OnFinished?.Invoke(this, new BackgroundTaskFinishedEventArgs<TIn, TOut>(args, result));
			}
			catch (Exception ex)
			{
				this._logger.LogError("Background task {Guid} failed", guid);
				this._logger.LogError("{Message}", ex.Message);
				this.OnFailed?.Invoke(this, new BackgroundTaskFailedEventArgs<TIn>(args, ex));
			}
		});

		this._logger.LogInformation("Background task {Guid} started", guid);
		this.OnStarted?.Invoke(this, new BackgroundTaskStartedEventArgs<TIn>(args));
	}

	#endregion
}