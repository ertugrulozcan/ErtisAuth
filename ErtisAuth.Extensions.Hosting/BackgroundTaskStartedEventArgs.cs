namespace ErtisAuth.Extensions.Hosting;

public class BackgroundTaskStartedEventArgs<TIn> : EventArgs
{
	#region Properties

	public TIn? InitialArgs { get; }

	#endregion
	
	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="args"></param>
	public BackgroundTaskStartedEventArgs(TIn? args)
	{
		this.InitialArgs = args;
	}

	#endregion
}