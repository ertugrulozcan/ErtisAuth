using System.Text.Json.Serialization;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Webhooks;

public class Webhook : MembershipBoundedResource, IHasSysInfo
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	[BsonElement("name")]
	public required string Name { get; set; }
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	[BsonElement("description")]
	public string? Description { get; set; }
	
	[JsonProperty("event")]
	[JsonPropertyName("event")]
	[BsonElement("event")]
	public required string Event { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public ErtisAuthEventType? EventType
	{
		get
		{
			if (Enum.GetNames(typeof(ErtisAuthEventType)).Any(x => x == this.Event))
			{
				return (ErtisAuthEventType) Enum.Parse(typeof(ErtisAuthEventType), this.Event);
			}
			else
			{
				return null;
			}
		}
	}
	
	[JsonProperty("status")]
	[JsonPropertyName("status")]
	[BsonElement("status")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public WebhookStatus? Status { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	[NewtonsoftJsonIgnore]
	public bool IsActive => this.Status == WebhookStatus.Active;
	
	[JsonProperty("request")]
	[JsonPropertyName("request")]
	[BsonElement("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonProperty("try_count")]
	[JsonPropertyName("try_count")]
	[BsonElement("try_count")]
	public int TryCount { get; set; }
	
	[JsonProperty("sys")]
	[JsonPropertyName("sys")]
	[BsonElement("sys")]
	public SysModel? Sys { get; set; }
	
	#endregion
}