using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Mailing.SendGrid;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ErtisAuth.Extensions.Mailing.Services;

public class SendGridService : IRawMailService<SendGridProvider>
{
	#region Methods
	
	public MailProviderType GetProviderType()
	{
		return MailProviderType.SendGrid;
	}
	
	public async Task SendMailAsync(
		SendGridProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(provider.ApiKey))
		{
			throw new Exception("SendGrid ApiKey is null or empty");
		}
		
		var client = new SendGridClient(provider.ApiKey);
		var email = new SendGridMessage
		{
			From = new EmailAddress(fromAddress, fromName),
			Subject = subject,
			HtmlContent = htmlBody
		};
		
		email.AddTos(recipients.Select(x => new EmailAddress(x.EmailAddress, x.DisplayName)).ToList());
		var response = await client.SendEmailAsync(email, cancellationToken: cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Error sending email: {response.StatusCode}");
		}
	}
	
	#endregion
}