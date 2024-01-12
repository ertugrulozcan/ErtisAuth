using System.Collections.Generic;
using Ertis.MongoDB.Attributes;
using ErtisAuth.Dto.Models.Resources;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Users
{
	public class UserDto : EntityBase, IHasMembership, IHasSysDto
	{
		#region Properties
		
		[Searchable]
		[BsonElement("firstname")]
		public string FirstName { get; set; }
		
		[Searchable]
		[BsonElement("lastname")]
		public string LastName { get; set; }
		
		[Searchable]
		[BsonElement("username")]
		public string Username { get; set; }
		
		[Searchable]
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
		
		[BsonElement("user_type")]
		public string UserType { get; set; }
		
		[BsonElement("source_provider")]
		public string SourceProvider { get; set; }
		
		[BsonElement("connected_accounts")]
		public ProviderAccountInfo[] ConnectedAccounts { get; set; }
		
		[BsonElement("is_active")]
		public bool IsActive { get; set; }
		
		[BsonElement("password_hash")]
		public string PasswordHash { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }

		#endregion
	}
}