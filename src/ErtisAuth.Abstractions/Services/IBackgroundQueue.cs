namespace ErtisAuth.Abstractions.Services;

/// <summary>
/// An in-memory queue of the work which is done in the background by a queue worker (e.g. hook mails, webhook calls)
/// </summary>
public interface IBackgroundQueue<T>
{
	#region Properties
	
	int Count { get; }
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Returns false when the queue is full or completed (on shutdown)
	/// </summary>
	bool TryEnqueue(T item);
	
	/// <summary>
	/// Reads the items until the queue is completed and drained
	/// </summary>
	IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Accepts no more items; the reader drains the queued ones
	/// </summary>
	void Complete();
	
	#endregion
}
