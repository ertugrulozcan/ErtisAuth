using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity
{
	public class ActiveTokenDto : TokenDto
	{
		#region Properties

		[BsonElement("user_id")]
		public string UserId { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("expire_time")]
		[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
		public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
		
		#endregion
	}
}