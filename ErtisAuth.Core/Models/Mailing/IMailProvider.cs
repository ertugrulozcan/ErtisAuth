using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Mailing;

public interface IMailProvider
{
	#region Properties
	
	[JsonPropertyName("guid")]
	[BsonElement("guid")]
	string? Guid { get; }
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	MailProviderType Type { get; }
	
	[JsonPropertyName("deliveryMode")]
	[BsonElement("deliveryMode")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	DeliveryMode DeliveryMode { get; }
	
	[JsonPropertyName("name")]
	[BsonElement("name")]
	string Name { get; }
	
	[JsonPropertyName("slug")]
	[BsonElement("slug")]
	string Slug { get; }
	
	#endregion
}