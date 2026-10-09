using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services;

public interface IMailHookService : IMembershipBoundedCrudService<MailHook>
{
	/// <summary>
	/// Queues the hook mail without waiting; it is sent in the background by the mail hook worker
	/// </summary>
	void QueueHookMail(
		MailHook mailHook,
		string userId,
		string membershipId,
		object? payload);
	
	/// <summary>
	/// Sends a queued hook mail (called by the mail hook worker); the failures are reported with the MailhookMailFailed event and logged, not thrown
	/// </summary>
	Task SendHookMailAsync(HookMail mail, CancellationToken cancellationToken = default);
	
	Task<MailHook?> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default);
	
	Task<MailHook?> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default);
}
