using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Events
{
	public class EventDto : EntityBase
	{
		#region Properties

		[BsonElement("event_type")]
		public string EventType { get; set; }
		
		[BsonElement("user_id")]
		public string UserId { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("document")]
		[BsonIgnoreIfNull]
		public BsonDocument Document { get; set; }
		
		[BsonElement("prior")]
		[BsonIgnoreIfNull]
		public BsonDocument Prior { get; set; }
		
		[BsonElement("event_time")]
		public DateTime EventTime { get; set; }

		#endregion
	}
}