// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Hosting;

public class BackgroundTaskFinishedEventArgs<TIn> : EventArgs
{
	#region Properties
	
	public TIn? InitialArgs { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="args"></param>
	public BackgroundTaskFinishedEventArgs(TIn? args)
	{
		this.InitialArgs = args;
	}
	
	#endregion
}

public class BackgroundTaskFinishedEventArgs<TIn, TOut> : EventArgs
{
	#region Properties
	
	public TIn? InitialArgs { get; }
	
	public TOut? Result { get; }
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="args"></param>
	/// <param name="result"></param>
	public BackgroundTaskFinishedEventArgs(TIn? args, TOut? result)
	{
		this.InitialArgs = args;
		this.Result = result;
	}
	
	#endregion
}