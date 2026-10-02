using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Mailing;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// Sends the queued hook mails (the mail providers rate limit the connections, so only a few are sent at once)
/// </summary>
public class MailHookBackgroundService : BaseQueueBackgroundService<HookMail>
{
	#region Services
	
	private readonly IMailHookService _mailHookService;
	
	#endregion
	
	#region Properties
	
	protected override int MaxConcurrency => 4;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mailHookQueue"></param>
	/// <param name="mailHookService"></param>
	/// <param name="logger"></param>
	public MailHookBackgroundService(
		IBackgroundQueue<HookMail> mailHookQueue,
		IMailHookService mailHookService,
		ILogger<MailHookBackgroundService> logger) : base(mailHookQueue, logger)
	{
		this._mailHookService = mailHookService;
	}
	
	#endregion
	
	#region Methods
	
	protected override Task ProcessAsync(HookMail item, CancellationToken cancellationToken)
	{
		return this._mailHookService.SendHookMailAsync(item, cancellationToken);
	}
	
	#endregion
}
