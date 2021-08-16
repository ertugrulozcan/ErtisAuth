using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Resources
{
	public class SysModelDto
	{
		#region Properties

		[BsonIgnoreIfNull]
		[BsonElement("created_at")]
		[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
		public DateTime? CreatedAt { get; set; }
		
		[BsonIgnoreIfNull]
		[BsonElement("created_by")]
		public string CreatedBy { get; set; }
		
		[BsonIgnoreIfNull]
		[BsonElement("modified_at")]
		[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
		public DateTime? ModifiedAt { get; set; }
		
		[BsonIgnoreIfNull]
		[BsonElement("modified_by")]
		public string ModifiedBy { get; set; }

		#endregion
	}
}