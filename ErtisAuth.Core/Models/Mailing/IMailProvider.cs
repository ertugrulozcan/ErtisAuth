using System.Text.Json.Serialization;
using Ertis.Net.Rest;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Mailing;

public interface IMailProvider
{
	#region Properties
	
	[JsonProperty("guid")]
	[JsonPropertyName("guid")]
	[BsonElement("guid")]
	string? Guid { get; }
	
	[JsonProperty("type")]
	[JsonPropertyName("type")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	MailProviderType Type { get; }
	
	[JsonProperty("deliveryMode")]
	[JsonPropertyName("deliveryMode")]
	[BsonElement("deliveryMode")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	DeliveryMode DeliveryMode { get; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	[BsonElement("name")]
	string Name { get; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	[BsonElement("slug")]
	string Slug { get; }
	
	#endregion
	
	#region Methods
	
	Task SendMailAsync(
		ISystemRestHandler restHandler, 
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string htmlBody,
		CancellationToken cancellationToken = default);
	
	Task SendMailWithTemplateAsync(
		ISystemRestHandler restHandler,
		string fromName,
		string fromAddress,
		IEnumerable<Recipient> recipients,
		string subject,
		string templateId,
		IDictionary<string, string> arguments,
		CancellationToken cancellationToken = default);
	
	#endregion
}