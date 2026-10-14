using ErtisAuth.Core.Events;
using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services;

public interface IMailService
{
	#region Events
	
	/// <summary>
	/// Raised after every sent mail, queued or not
	/// </summary>
	event EventHandler<MailEventArgs>? OnMailSent;
	
	/// <summary>
	/// Raised after every mail which could not be sent, queued or not
	/// </summary>
	event EventHandler<MailEventArgs>? OnMailFailed;
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Queues the mail without waiting; it is sent in the background by the mail worker.
	/// Queuing is best effort: returns false (and logs) when the mail could not be queued, e.g. the queue is full or closed on shutdown.
	/// </summary>
	/// <param name="mail"></param>
	/// <param name="onCompleted">Called after the sending, with the error if the mail could not be sent</param>
	bool QueueMail(MailMessage mail, Func<Exception?, Task>? onCompleted = null);
	
	/// <summary>
	/// Sends the mail with the membership's mail provider; the failures are thrown
	/// </summary>
	Task SendMailAsync(MailMessage mail, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Sends a queued mail and calls its completion callback (called by the mail worker); the failures are logged, not thrown
	/// </summary>
	Task SendQueuedMailAsync(QueuedMail queuedMail, CancellationToken cancellationToken = default);
	
	#endregion
}
