using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Extensions.Mailing.Services.Interfaces;

public interface IRawMailService : IMailService
{
	#region Methods
	
	Task SendMailAsync(
		IMailProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default);
	
	#endregion
}

/// <summary>
/// A raw mail service of a provider type; it implements only the typed method, the provider of the non-generic method is cast here once for all services
/// </summary>
public interface IRawMailService<in TProvider> : IRawMailService where TProvider : IMailProvider
{
	#region Methods
	
	Task SendMailAsync(
		TProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default);
	
	Task IRawMailService.SendMailAsync(
		IMailProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken)
	{
		if (provider is not TProvider typedProvider)
		{
			throw new ArgumentException($"The {this.GetType().Name} can not send mails with a {provider.GetType().Name} (expected {typeof(TProvider).Name})", nameof(provider));
		}
		
		return this.SendMailAsync(typedProvider, fromName, fromAddress, recipients, subject, htmlBody, cancellationToken);
	}
	
	#endregion
}