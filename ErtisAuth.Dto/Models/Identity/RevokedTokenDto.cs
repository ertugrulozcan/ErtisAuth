using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity
{
	public class RevokedTokenDto : EntityBase
	{
		#region Properties

		[BsonElement("token")]
		public string Token { get; set; }
		
		[BsonElement("user_id")]
		public string UserId { get; set; }
		
		[BsonElement("revoked_at")]
		public DateTime RevokedAt { get; set; }

		#endregion
	}
}