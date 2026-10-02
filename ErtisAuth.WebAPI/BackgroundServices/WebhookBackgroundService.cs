using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Webhooks;

namespace ErtisAuth.WebAPI.BackgroundServices;

/// <summary>
/// Executes the queued webhook calls (a slow receiver holds a slot until its timeout, so more calls than mails run at once)
/// </summary>
public class WebhookBackgroundService : BaseQueueBackgroundService<WebhookCall>
{
	#region Services
	
	private readonly IWebhookService _webhookService;
	
	#endregion
	
	#region Properties
	
	protected override int MaxConcurrency => 8;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="webhookQueue"></param>
	/// <param name="webhookService"></param>
	/// <param name="logger"></param>
	public WebhookBackgroundService(
		IBackgroundQueue<WebhookCall> webhookQueue,
		IWebhookService webhookService,
		ILogger<WebhookBackgroundService> logger) : base(webhookQueue, logger)
	{
		this._webhookService = webhookService;
	}
	
	#endregion
	
	#region Methods
	
	protected override Task ProcessAsync(WebhookCall item, CancellationToken cancellationToken)
	{
		return this._webhookService.ExecuteWebhookAsync(item, cancellationToken);
	}
	
	#endregion
}
