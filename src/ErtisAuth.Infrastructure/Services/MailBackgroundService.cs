using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// Sends the queued mails (the mail providers rate limit the connections, so only a few are sent at once)
/// </summary>
public class MailBackgroundService : BaseQueueBackgroundService<QueuedMail>
{
	#region Services
	
	private readonly IMailService _mailService;
	
	#endregion
	
	#region Properties
	
	protected override int MaxConcurrency => 4;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mailQueue"></param>
	/// <param name="mailService"></param>
	/// <param name="logger"></param>
	public MailBackgroundService(
		IBackgroundQueue<QueuedMail> mailQueue,
		IMailService mailService,
		ILogger<MailBackgroundService> logger) : base(mailQueue, logger)
	{
		this._mailService = mailService;
	}
	
	#endregion
	
	#region Methods
	
	protected override Task ProcessAsync(QueuedMail item, CancellationToken cancellationToken)
	{
		return this._mailService.SendQueuedMailAsync(item, cancellationToken);
	}
	
	#endregion
}