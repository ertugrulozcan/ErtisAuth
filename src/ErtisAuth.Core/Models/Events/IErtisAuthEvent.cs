using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Events;

public interface IErtisAuthEvent
{
	#region Properties
	
	[JsonPropertyName("utilizer_id")]
	[BsonElement("utilizer_id")]
	string UtilizerId { get; set; }
	
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	string MembershipId { get; set; }
	
	[JsonPropertyName("document")]
	[BsonElement("document")]
	dynamic? Document { get; set; }
	
	[JsonPropertyName("prior")]
	[BsonElement("prior")]
	dynamic? Prior { get; set; }
	
	[JsonPropertyName("event_time")]
	[BsonElement("event_time")]
	DateTime EventTime { get; set; }
	
	#endregion
}