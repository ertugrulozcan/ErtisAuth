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
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ErtisAuth.Extensions.Mailkit.Providers;

public class SendGridProvider : IMailProvider
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
	public MailProviderType Type => MailProviderType.SendGrid;

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
	
	[JsonProperty("apiKey")]
	[JsonPropertyName("apiKey")]
	public string ApiKey { get; set; }
	
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
		if (string.IsNullOrEmpty(this.ApiKey))
		{
			throw new Exception("SendGrid ApiKey is null or empty");
		}
		
		var client = new SendGridClient(this.ApiKey);
		var email = new SendGridMessage()
		{
			From = new EmailAddress(fromAddress, fromName),
			Subject = subject,
			HtmlContent = htmlBody
		};
		
		email.AddTos(recipients.Select(x => new EmailAddress(x.EmailAddress, x.DisplayName)).ToList());
		await client.SendEmailAsync(email, cancellationToken: cancellationToken);
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