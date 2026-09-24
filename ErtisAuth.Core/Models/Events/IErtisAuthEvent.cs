using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Events;

public interface IErtisAuthEvent
{
	#region Properties
	
	[JsonProperty("utilizer_id")]
	[JsonPropertyName("utilizer_id")]
	[BsonElement("utilizer_id")]
	string UtilizerId { get; set; }
	
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	string MembershipId { get; set; }
	
	[JsonProperty("document")]
	[JsonPropertyName("document")]
	[BsonElement("document")]
	dynamic? Document { get; set; }
	
	[JsonProperty("prior")]
	[JsonPropertyName("prior")]
	[BsonElement("prior")]
	dynamic? Prior { get; set; }
	
	[JsonProperty("event_time")]
	[JsonPropertyName("event_time")]
	[BsonElement("event_time")]
	DateTime EventTime { get; set; }
	
	#endregion
}