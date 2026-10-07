using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Extensions.Mailing.Services.Interfaces;

public interface ITemplateMailService : IMailService
{
	#region Methods
	
	Task SendMailWithTemplateAsync(
		IMailProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken = default);
	
	#endregion
}

/// <summary>
/// A template mail service of a provider type; it implements only the typed method, the provider of the non-generic method is cast here once for all services
/// </summary>
public interface ITemplateMailService<in TProvider> : ITemplateMailService where TProvider : IMailProvider
{
	#region Methods
	
	Task SendMailWithTemplateAsync(
		TProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken = default);
	
	Task ITemplateMailService.SendMailWithTemplateAsync(
		IMailProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken)
	{
		if (provider is not TProvider typedProvider)
		{
			throw new ArgumentException($"The {this.GetType().Name} can not send mails with a {provider.GetType().Name} (expected {typeof(TProvider).Name})", nameof(provider));
		}
		
		return this.SendMailWithTemplateAsync(typedProvider, fromName, fromAddress, recipients, subject, templateId, arguments, cancellationToken);
	}
	
	#endregion
}