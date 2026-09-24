using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Mailing;

public class SmtpServerProvider : IMailProvider
{
	#region Properties
	
	[JsonProperty("guid")]
	[JsonPropertyName("guid")]
	[BsonElement("guid")]
	public string? Guid { get; set; }
	
	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public MailProviderType Type => MailProviderType.SmtpServer;
	
	[JsonProperty("deliveryMode")]
	[JsonPropertyName("deliveryMode")]
	[BsonElement("deliveryMode")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public DeliveryMode DeliveryMode => DeliveryMode.Default;
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	[BsonElement("slug")]
	public string Slug
	{
		get
		{
			if (string.IsNullOrEmpty(field))
			{
				field = Slugifier.Slugify(this.Name, Slugifier.Options.Ignore('_'));
			}
			
			return field;
		}
	}
	
	[JsonProperty("host")]
	[JsonPropertyName("host")]
	[BsonElement("host")]
	public required string Host { get; set; }
	
	[JsonProperty("port")]
	[JsonPropertyName("port")]
	[BsonElement("port")]
	public required int Port { get; set; }
	
	[JsonProperty("tls_enabled")]
	[JsonPropertyName("tls_enabled")]
	[BsonElement("tls_enabled")]
	public bool TlsEnabled { get; set; }
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public required string Username { get; set; }
	
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	[BsonElement("password")]
	public required string Password { get; set; }
	
	#endregion
	
	#region Methods
	
	public Task SendMailAsync(
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}
	
	/*
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
		
		using var client = new SmtpClient();
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
	*/
	
	#endregion
}