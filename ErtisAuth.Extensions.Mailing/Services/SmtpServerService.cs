using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Mailing.Services.Interfaces;
using ErtisAuth.Extensions.Mailing.SmtpServer;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ErtisAuth.Extensions.Mailing.Services;

public class SmtpServerService : IRawMailService<SmtpServerProvider>
{
	#region Methods
	
	public MailProviderType GetProviderType()
	{
		return MailProviderType.SmtpServer;
	}
	
	public async Task SendMailAsync(
		SmtpServerProvider provider,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(fromName, fromAddress));
		message.To.AddRange(recipients.Select(x => new MailboxAddress(x.DisplayName, x.EmailAddress)));
		message.Subject = subject;
		
		var builder = new BodyBuilder { HtmlBody = htmlBody };
		message.Body = builder.ToMessageBody();
		
		using var client = new SmtpClient();
		if (provider.TlsEnabled)
		{
			await client.ConnectAsync(provider.Host, provider.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken: cancellationToken);
		}
		else
		{
			await client.ConnectAsync(provider.Host, provider.Port, cancellationToken: cancellationToken);
		}
		
		await client.AuthenticateAsync(provider.Username, provider.Password, cancellationToken: cancellationToken);
		await client.SendAsync(message, cancellationToken: cancellationToken);
		await client.DisconnectAsync(true, cancellationToken: cancellationToken);
	}
	
	#endregion
}