// ReSharper disable UnusedType.Global
namespace ErtisAuth.Extensions.Hosting;

public abstract class BackgroundWorker<TIn> : IBackgroundWorker<TIn> where TIn : class
{
	#region Services
	
	private readonly IBackgroundTaskQueue _taskQueue;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="taskQueue"></param>
	protected BackgroundWorker(IBackgroundTaskQueue taskQueue)
	{
		this._taskQueue = taskQueue;
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
		await this._taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
		{
			try
			{
				await this.ExecuteAsync(args, cancellationToken);
				this.OnFinished?.Invoke(this, new BackgroundTaskFinishedEventArgs<TIn>(args));
			}
			catch (Exception ex)
			{
				this.OnFailed?.Invoke(this, new BackgroundTaskFailedEventArgs<TIn>(args, ex));
			}
		});
		
		this.OnStarted?.Invoke(this, new BackgroundTaskStartedEventArgs<TIn>(args));
	}
	
	#endregion
}

public abstract class BackgroundWorker<TIn, TOut> : IBackgroundWorker<TIn, TOut> where TIn : class
{
	#region Services
	
	private readonly IBackgroundTaskQueue _taskQueue;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="taskQueue"></param>
	protected BackgroundWorker(IBackgroundTaskQueue taskQueue)
	{
		this._taskQueue = taskQueue;
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
		await this._taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
		{
			try
			{
				var result = await this.ExecuteAsync(args, cancellationToken);
				this.OnFinished?.Invoke(this, new BackgroundTaskFinishedEventArgs<TIn, TOut>(args, result));
			}
			catch (Exception ex)
			{
				this.OnFailed?.Invoke(this, new BackgroundTaskFailedEventArgs<TIn>(args, ex));
			}
		});
		
		this.OnStarted?.Invoke(this, new BackgroundTaskStartedEventArgs<TIn>(args));
	}
	
	#endregion
}