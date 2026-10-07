using System.Threading.Channels;
using ErtisAuth.Abstractions.Services;

namespace ErtisAuth.Infrastructure.Services;

public class BackgroundQueue<T> : IBackgroundQueue<T>
{
	#region Constants
	
	/// <summary>
	/// Bounds the memory under a burst of events; an item which does not fit is rejected (logged by the caller)
	/// </summary>
	public const int Capacity = 10_000;
	
	#endregion
	
	#region Fields
	
	private readonly Channel<T> _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(Capacity)
	{
		FullMode = BoundedChannelFullMode.Wait
	});
	
	#endregion
	
	#region Properties
	
	public int Count => this._channel.Reader.Count;
	
	#endregion
	
	#region Methods
	
	public bool TryEnqueue(T item)
	{
		return this._channel.Writer.TryWrite(item);
	}
	
	public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default)
	{
		return this._channel.Reader.ReadAllAsync(cancellationToken);
	}
	
	public void Complete()
	{
		this._channel.Writer.TryComplete();
	}
	
	#endregion
}
