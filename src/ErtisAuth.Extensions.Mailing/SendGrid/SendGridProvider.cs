using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using ErtisAuth.Core.Models.Mailing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Mailing.SendGrid;

public class SendGridProvider : IMailProvider
{
	#region Properties
	
	[JsonPropertyName("guid")]
	[BsonElement("guid")]
	public string? Guid { get; set; }
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	public MailProviderType Type => MailProviderType.SendGrid;
	
	[JsonPropertyName("deliveryMode")]
	[BsonElement("deliveryMode")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public DeliveryMode DeliveryMode => DeliveryMode.Default; 
	
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
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
	
	[JsonPropertyName("apiKey")]
	[BsonElement("apiKey")]
	public string? ApiKey { get; set; }
	
	#endregion
}