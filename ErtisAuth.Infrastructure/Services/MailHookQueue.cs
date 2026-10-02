using System.Threading.Channels;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Infrastructure.Services;

public class MailHookQueue : IMailHookQueue
{
	#region Constants
	
	/// <summary>
	/// Bounds the memory under a burst of events; a mail which does not fit is rejected (logged by the caller)
	/// </summary>
	public const int Capacity = 10_000;
	
	#endregion
	
	#region Fields
	
	private readonly Channel<HookMail> _channel = Channel.CreateBounded<HookMail>(new BoundedChannelOptions(Capacity)
	{
		FullMode = BoundedChannelFullMode.Wait
	});
	
	#endregion
	
	#region Properties
	
	public int Count => this._channel.Reader.Count;
	
	#endregion
	
	#region Methods
	
	public bool TryEnqueue(HookMail mail)
	{
		return this._channel.Writer.TryWrite(mail);
	}
	
	public IAsyncEnumerable<HookMail> ReadAllAsync(CancellationToken cancellationToken = default)
	{
		return this._channel.Reader.ReadAllAsync(cancellationToken);
	}
	
	public void Complete()
	{
		this._channel.Writer.TryComplete();
	}
	
	#endregion
}
