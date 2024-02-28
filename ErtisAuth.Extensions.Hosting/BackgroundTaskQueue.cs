using System.Threading.Channels;

namespace ErtisAuth.Extensions.Hosting;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
	#region Services

	private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="capacity"></param>
	public BackgroundTaskQueue(int capacity)
	{
		var options = new BoundedChannelOptions(capacity)
		{
			FullMode = BoundedChannelFullMode.Wait
		};

		this._queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
	}

	#endregion

	#region Methods

	public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
	{
		if (workItem == null)
		{
			throw new ArgumentNullException(nameof(workItem));
		}

		await this._queue.Writer.WriteAsync(workItem);
	}

	public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
	{
		var workItem = await this._queue.Reader.ReadAsync(cancellationToken);
		return workItem;
	}

	#endregion
}