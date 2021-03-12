using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Memberships
{
	public class MembershipDto : EntityBase, IHasSysDto
	{
		#region Properties
		
		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("expires_in")]
		public int ExpiresIn { get; set; }
		
		[BsonElement("refresh_token_expires_in")]
		public int RefreshTokenExpiresIn { get; set; }
		
		[BsonElement("secret_key")]
		public string SecretKey { get; set; }
		
		[BsonElement("hash_algorithm")]
		public string HashAlgorithm { get; set; }

		[BsonElement("encoding")]
		public string DefaultEncoding { get; set; }

		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}