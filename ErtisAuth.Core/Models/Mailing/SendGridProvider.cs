using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Mailing;

public class SendGridProvider : IMailProvider
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
	public MailProviderType Type => MailProviderType.SendGrid;
	
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
	
	[JsonProperty("apiKey")]
	[JsonPropertyName("apiKey")]
	[BsonElement("apiKey")]
	public string? ApiKey { get; set; }
	
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
		if (string.IsNullOrEmpty(this.ApiKey))
		{
			throw new Exception("SendGrid ApiKey is null or empty");
		}
		
		var client = new SendGridClient(this.ApiKey);
		var email = new SendGridMessage
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
	*/
	
	#endregion
}