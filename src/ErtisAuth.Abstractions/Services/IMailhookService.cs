using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services;

public interface IMailhookService : IMembershipBoundedCrudService<Mailhook>
{
	/// <summary>
	/// Renders the hook mail with the payload and queues it to the mail service (a passive hook is not queued);
	/// the outcome is reported with the MailhookMailSent or MailhookMailFailed event. The failures are logged, not thrown.
	/// </summary>
	Task QueueHookMailAsync(
		Mailhook mailhook,
		string userId,
		string membershipId,
		object? payload,
		CancellationToken cancellationToken = default);
	
	Task<Mailhook?> GetUserActivationMailhookAsync(string membershipId, CancellationToken cancellationToken = default);
	
	Task<Mailhook?> GetResetPasswordMailhookAsync(string membershipId, CancellationToken cancellationToken = default);
}
