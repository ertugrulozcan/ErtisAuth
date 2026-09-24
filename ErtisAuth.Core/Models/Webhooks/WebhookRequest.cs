using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks;

public class WebhookRequest
{
	#region Properties
	
	[JsonProperty("method")]
	[JsonPropertyName("method")]
	[BsonElement("method")]
	public required string Method { get; set; }
	
	[JsonProperty("url")]
	[JsonPropertyName("url")]
	[BsonElement("url")]
	public required string Url { get; set; }
	
	[JsonProperty("headers")]
	[JsonPropertyName("headers")]
	[BsonElement("headers")]
	public Dictionary<string, object>? Headers { get; set; }
	
	[JsonProperty("body")]
	[JsonPropertyName("body")]
	[BsonElement("body")]
	public dynamic? Body { get; set; }
	
	#endregion
}