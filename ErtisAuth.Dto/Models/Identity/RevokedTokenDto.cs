using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity
{
	public class RevokedTokenDto : EntityBase, IHasMembership
	{
		#region Properties

		[BsonElement("token")]
		public ActiveTokenDto Token { get; set; }
		
		[BsonElement("user_id")]
		public string UserId { get; set; }
		
		[BsonElement("username")]
		public string UserName { get; set; }
		
		[BsonElement("email_address")]
		public string EmailAddress { get; set; }
		
		[BsonElement("first_name")]
		public string FirstName { get; set; }
		
		[BsonElement("last_name")]
		public string LastName { get; set; }
		
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