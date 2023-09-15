using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ErtisAuth.Extensions.Mailkit.Providers;

public class SmtpServerProvider : IMailProvider
{
	#region Fields

	private string slug;
	
	#endregion
	
	#region Properties
	
	[JsonProperty("guid")]
	public string Guid { get; set; }

	[JsonProperty("type")]
	[JsonConverter(typeof(StringEnumConverter))]
	public MailProviderType Type => MailProviderType.SmtpServer;
	
	[JsonProperty("name")]
	public string Name { get; set; }
	
	[JsonProperty("slug")]
	public string Slug
	{
		get
		{
			if (string.IsNullOrEmpty(this.slug))
			{
				this.slug = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
			}

			return this.slug;
		}
	}
	
	[JsonProperty("host")]
	public string Host { get; set; }
        
	[JsonProperty("port")]
	public int Port { get; set; }
        
	[JsonProperty("tls_enabled")]
	public bool TlsEnabled { get; set; }
        
	[JsonProperty("username")]
	public string Username { get; set; }
        
	[JsonProperty("password")]
	public string Password { get; set; }

	#endregion
	
	#region Methods

	public async Task SendMailAsync(
		string fromName,
		string fromAddress,
		string toName,
		string toAddress,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(fromName, fromAddress));
		message.To.Add(new MailboxAddress(toName, toAddress));
		message.Subject = subject;

		var builder = new BodyBuilder { HtmlBody = htmlBody };
		message.Body = builder.ToMessageBody();

		using (var client = new SmtpClient())
		{
			if (this.TlsEnabled)
			{
				await client.ConnectAsync(this.Host, this.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken: cancellationToken);
			}
			else
			{
				await client.ConnectAsync(this.Host, this.Port, cancellationToken: cancellationToken);
			}

			await client.AuthenticateAsync(this.Username, this.Password, cancellationToken: cancellationToken);
			await client.SendAsync(message, cancellationToken: cancellationToken);
			await client.DisconnectAsync(true, cancellationToken: cancellationToken);
		}
	}

	#endregion
}