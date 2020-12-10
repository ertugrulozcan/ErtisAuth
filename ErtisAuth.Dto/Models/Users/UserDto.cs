using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Dto.Models.Users
{
	public class UserDto : EntityBase, IHasMembership
	{
		#region Properties
		
		[BsonElement("firstname")]
		public string FirstName { get; set; }
		
		[BsonElement("lastname")]
		public string LastName { get; set; }
		
		[JsonProperty("username")]
		public string Username { get; set; }
		
		[JsonProperty("email_address")]
		public string EmailAddress { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		#endregion
	}
}