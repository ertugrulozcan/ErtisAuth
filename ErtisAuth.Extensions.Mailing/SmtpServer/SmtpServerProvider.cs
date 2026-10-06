using System.Text.Json.Serialization;
using Ertis.Core.Helpers;
using ErtisAuth.Core.Models.Mailing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.Mailing.SmtpServer;

public class SmtpServerProvider : IMailProvider
{
	#region Properties
	
	[JsonPropertyName("guid")]
	[BsonElement("guid")]
	public string? Guid { get; set; }
	
	[JsonPropertyName("type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonIgnore]
	public MailProviderType Type => MailProviderType.SmtpServer;
	
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
	
	[JsonPropertyName("host")]
	[BsonElement("host")]
	public required string Host { get; set; }
	
	[JsonPropertyName("port")]
	[BsonElement("port")]
	public required int Port { get; set; }
	
	[JsonPropertyName("tls_enabled")]
	[BsonElement("tls_enabled")]
	public bool TlsEnabled { get; set; }
	
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public required string Username { get; set; }
	
	[JsonPropertyName("password")]
	[BsonElement("password")]
	public required string Password { get; set; }
	
	#endregion
}