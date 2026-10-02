using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services;

/// <summary>
/// The in-memory queue of the hook mails, which are sent in the background by the mail hook worker
/// </summary>
public interface IMailHookQueue
{
	#region Properties
	
	int Count { get; }
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Returns false when the queue is full or completed (on shutdown)
	/// </summary>
	bool TryEnqueue(HookMail mail);
	
	/// <summary>
	/// Reads the mails until the queue is completed and drained
	/// </summary>
	IAsyncEnumerable<HookMail> ReadAllAsync(CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Accepts no more mails; the reader drains the queued ones
	/// </summary>
	void Complete();
	
	#endregion
}
