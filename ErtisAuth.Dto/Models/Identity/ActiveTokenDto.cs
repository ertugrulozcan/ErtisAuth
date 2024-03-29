using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity
{
	public class ActiveTokenDto : TokenDto, IHasMembership
	{
		#region Properties

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
		
		[BsonElement("expire_time")]
		[BsonDateTimeOptions(Kind = DateTimeKind.Local)]
		public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
		
		[BsonElement("client_info")]
		public ClientInfoDto ClientInfo { get; set; }

		#endregion
	}
}