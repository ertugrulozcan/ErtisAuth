using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity
{
	public class RevokedTokenDto : EntityBase, IHasMembership
	{
		#region Properties

		[BsonElement("token")]
		public string Token { get; set; }
		
		[BsonElement("user_id")]
		public string UserId { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("token_type")]
		public string TokenType { get; set; }
		
		[BsonElement("revoked_at")]
		[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
		public DateTime RevokedAt { get; set; }

		#endregion
	}
}