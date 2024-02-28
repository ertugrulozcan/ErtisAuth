namespace ErtisAuth.Extensions.Hosting;

public class BackgroundTaskFailedEventArgs<TIn> : EventArgs
{
	#region Properties
	
	public TIn? InitialArgs { get; }

	public Exception Exception { get; }

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="args"></param>
	/// <param name="exception"></param>
	public BackgroundTaskFailedEventArgs(TIn? args, Exception exception)
	{
		this.InitialArgs = args;
		this.Exception = exception;
	}

	#endregion
}