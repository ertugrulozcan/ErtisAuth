using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Dto.Models.Events;

public class EventDto : EntityBase, IHasMembership
{
	#region Properties
	
	[BsonElement("event_type")]
	public required string EventType { get; set; }
	
	[BsonElement("utilizer_id")]
	public required string UtilizerId { get; set; }
	
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
	[BsonElement("document")]
	[BsonIgnoreIfNull]
	public BsonDocument? Document { get; set; }
	
	[BsonElement("prior")]
	[BsonIgnoreIfNull]
	public BsonDocument? Prior { get; set; }
	
	[BsonElement("event_time")]
	[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
	public DateTime EventTime { get; set; }
	
	[BsonElement("is_custom_event")] 
	public bool IsCustomEvent { get; set; }
	
	#endregion
}