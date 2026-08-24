// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global
namespace ErtisAuth.Extensions.Hosting;

public interface IBackgroundWorker<TIn> where TIn : class
{
	#region Events
	
	event EventHandler<BackgroundTaskStartedEventArgs<TIn>>? OnStarted;
	event EventHandler<BackgroundTaskFailedEventArgs<TIn>>? OnFailed;
	event EventHandler<BackgroundTaskFinishedEventArgs<TIn>>? OnFinished;
	
	#endregion
	
	#region Methods
	
	ValueTask StartAsync(TIn? args = null);
	
	#endregion
}

public interface IBackgroundWorker<TIn, TOut> where TIn : class
{
	#region Events
	
	event EventHandler<BackgroundTaskStartedEventArgs<TIn>>? OnStarted;
	event EventHandler<BackgroundTaskFailedEventArgs<TIn>>? OnFailed;
	event EventHandler<BackgroundTaskFinishedEventArgs<TIn, TOut>>? OnFinished;
	
	#endregion
	
	#region Methods
	
	ValueTask StartAsync(TIn? args = null);
	
	#endregion
}