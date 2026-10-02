using ErtisAuth.Core.Models.Webhooks;

namespace ErtisAuth.Abstractions.Services;

public interface IWebhookService : IMembershipBoundedCrudService<Webhook>
{
	/// <summary>
	/// Executes a queued webhook call (called by the webhook worker); the outcome is reported with the WebhookRequestSent / WebhookRequestFailed events and logged, not thrown
	/// </summary>
	Task ExecuteWebhookAsync(WebhookCall call, CancellationToken cancellationToken = default);
}
