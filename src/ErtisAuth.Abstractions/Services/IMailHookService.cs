using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services;

public interface IMailHookService : IMembershipBoundedCrudService<MailHook>
{
	/// <summary>
	/// Renders the hook mail with the payload and queues it to the mail service (a passive hook is not queued);
	/// the outcome is reported with the MailhookMailSent or MailhookMailFailed event. The failures are logged, not thrown.
	/// </summary>
	Task QueueHookMailAsync(
		MailHook mailHook,
		string userId,
		string membershipId,
		object? payload,
		CancellationToken cancellationToken = default);
	
	Task<MailHook?> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default);
	
	Task<MailHook?> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default);
}
