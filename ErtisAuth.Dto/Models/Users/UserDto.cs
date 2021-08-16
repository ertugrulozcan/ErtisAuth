using System.Collections.Generic;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Users
{
	public class UserDto : EntityBase, IHasMembership, IHasSysDto
	{
		#region Properties
		
		[BsonElement("firstname")]
		public string FirstName { get; set; }
		
		[BsonElement("lastname")]
		public string LastName { get; set; }
		
		[BsonElement("username")]
		public string Username { get; set; }
		
		[BsonElement("email_address")]
		public string EmailAddress { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("role")]
		public string Role { get; set; }
		
		[BsonElement("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[BsonElement("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		[BsonElement("password_hash")]
		public string PasswordHash { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}