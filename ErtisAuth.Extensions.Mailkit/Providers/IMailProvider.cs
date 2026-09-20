using System.Text.Json.Serialization;
using ErtisAuth.Extensions.Mailkit.Models;
using Newtonsoft.Json;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Extensions.Mailkit.Providers;

public interface IMailProvider
{
	#region Properties
	
	[JsonProperty("guid")]
	[JsonPropertyName("guid")]
	string? Guid { get; }
	
	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	MailProviderType Type { get; }
	
	[JsonProperty("deliveryMode")]
	[JsonPropertyName("deliveryMode")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	MailDeliveryMode DeliveryMode { get; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	string Name { get; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	string Slug { get; }
	
	#endregion
	
	#region Methods
	
	Task SendMailAsync(
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default);
	
	Task SendMailWithTemplateAsync(
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken = default);
	
	#endregion
}