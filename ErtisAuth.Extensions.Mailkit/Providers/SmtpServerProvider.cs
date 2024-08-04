using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Helpers;
using ErtisAuth.Extensions.Mailkit.Models;
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
	[JsonPropertyName("guid")]
	public string Guid { get; set; }

	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	public MailProviderType Type => MailProviderType.SmtpServer;
	
	[JsonProperty("deliveryMode")]
	[JsonPropertyName("deliveryMode")]
	[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	public MailDeliveryMode DeliveryMode => MailDeliveryMode.Default;
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
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
	[JsonPropertyName("host")]
	public string Host { get; set; }
        
	[JsonProperty("port")]
	[JsonPropertyName("port")]
	public int Port { get; set; }
        
	[JsonProperty("tls_enabled")]
	[JsonPropertyName("tls_enabled")]
	public bool TlsEnabled { get; set; }
        
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	public string Username { get; set; }
        
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	public string Password { get; set; }

	#endregion
	
	#region Methods

	public async Task SendMailAsync(
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
	
	public Task SendMailWithTemplateAsync(
		string fromName, 
		string fromAddress, 
		IEnumerable<Recipient> recipients, 
		string subject, 
		string templateId, 
		IDictionary<string, string> arguments, 
		CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException("This provider is not supported with template mailing");
	}

	#endregion
}