using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Events;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// Sends the mails with the mail providers of the memberships, directly or through the mail queue
/// </summary>
public class MailService : IMailService
{
	#region Services
	
	private readonly IMembershipService _membershipService;
	private readonly IEnumerable<IProviderMailService> _providerMailServices;
	private readonly IBackgroundQueue<QueuedMail> _mailQueue;
	private readonly ILogger<MailService> _logger;
	
	#endregion
	
	#region Events
	
	public event EventHandler<MailEventArgs>? OnMailSent;
	
	public event EventHandler<MailEventArgs>? OnMailFailed;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="membershipService"></param>
	/// <param name="providerMailServices"></param>
	/// <param name="mailQueue"></param>
	/// <param name="logger"></param>
	public MailService(
		IMembershipService membershipService,
		IEnumerable<IProviderMailService> providerMailServices,
		IBackgroundQueue<QueuedMail> mailQueue,
		ILogger<MailService> logger)
	{
		this._membershipService = membershipService;
		this._providerMailServices = providerMailServices;
		this._mailQueue = mailQueue;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	public bool QueueMail(MailMessage mail, Func<Exception?, Task>? onCompleted = null)
	{
		try
		{
			if (this._mailQueue.TryEnqueue(new QueuedMail(mail, onCompleted)))
			{
				return true;
			}
			
			this._logger.LogError("The mail '{Subject}' could not be queued: the mail queue is full or closed", mail.Subject);
		}
		catch (Exception ex)
		{
			// Queuing is best effort like the sending: the flow which queues the mail (e.g. a password reset) does not fail
			this._logger.LogError(ex, "The mail '{Subject}' could not be queued", mail.Subject);
		}
		
		return false;
	}
	
	public async Task SendQueuedMailAsync(QueuedMail queuedMail, CancellationToken cancellationToken = default)
	{
		Exception? error = null;
		try
		{
			await this.SendMailAsync(queuedMail.Mail, cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			error = ex;
			this._logger.LogError(ex, "The queued mail '{Subject}' could not be sent", queuedMail.Mail.Subject);
		}
		
		if (queuedMail.OnCompleted != null)
		{
			try
			{
				await queuedMail.OnCompleted(error);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "The completion callback of the queued mail '{Subject}' failed", queuedMail.Mail.Subject);
			}
		}
	}
	
	public async Task SendMailAsync(MailMessage mail, CancellationToken cancellationToken = default)
	{
		try
		{
			var mailProvider = await this.GetMailProviderAsync(mail, cancellationToken: cancellationToken);
			await this.DeliverAsync(mailProvider, mail, cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			this.RaiseEvent(this.OnMailFailed, new MailEventArgs(mail, ex));
			throw;
		}
		
		this.RaiseEvent(this.OnMailSent, new MailEventArgs(mail));
	}
	
	private async Task<IMailProvider> GetMailProviderAsync(MailMessage mail, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(mail.MembershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(mail.MembershipId);
		}
		
		var mailProvider = membership.MailProviders?.FirstOrDefault(x => x.Slug == mail.MailProvider);
		if (mailProvider == null)
		{
			throw new InvalidOperationException($"The mail provider '{mail.MailProvider}' is not defined in the membership");
		}
		
		return mailProvider;
	}
	
	private async Task DeliverAsync(IMailProvider mailProvider, MailMessage mail, CancellationToken cancellationToken = default)
	{
		var service = this._providerMailServices.FirstOrDefault(x => x.GetProviderType() == mailProvider.Type);
		if (service == null)
		{
			throw new InvalidOperationException($"No mail service is registered for the {mailProvider.Type} provider");
		}
		
		switch (mailProvider.DeliveryMode)
		{
			case DeliveryMode.Default:
			case DeliveryMode.Raw:
				if (service is IRawMailService rawMailService)
				{
					await rawMailService.SendMailAsync(
						mailProvider,
						mail.FromName,
						mail.FromAddress,
						mail.Recipients,
						mail.Subject,
						mail.HtmlBody ?? string.Empty,
						cancellationToken: cancellationToken);
				}
				else
				{
					throw new InvalidOperationException($"The {mailProvider.Type} mail service does not support raw mails");
				}
			break;
			case DeliveryMode.Template:
				if (service is ITemplateMailService templateMailService)
				{
					await templateMailService.SendMailWithTemplateAsync(
						mailProvider,
						mail.FromName,
						mail.FromAddress,
						mail.Recipients,
						mail.Subject,
						mail.TemplateId ?? string.Empty,
						mail.TemplateArguments ?? new Dictionary<string, string>(),
						cancellationToken: cancellationToken);
				}
				else
				{
					throw new InvalidOperationException($"The {mailProvider.Type} mail service does not support template mails");
				}
			break;
		}
	}
	
	/// <summary>
	/// A failing subscriber must not change the result of the sending
	/// </summary>
	private void RaiseEvent(EventHandler<MailEventArgs>? handler, MailEventArgs eventArgs)
	{
		try
		{
			handler?.Invoke(this, eventArgs);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "A mail event subscriber failed");
		}
	}
	
	#endregion
}
