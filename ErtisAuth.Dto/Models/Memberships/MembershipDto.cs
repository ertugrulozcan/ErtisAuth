using System.Diagnostics.CodeAnalysis;
using Ertis.MongoDB.Attributes;
using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Memberships
{
	// ReSharper disable once ClassNeverInstantiated.Global
	// ReSharper disable PropertyCanBeMadeInitOnly.Global
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public class MembershipDto : EntityBase, IHasSysDto
	{
		#region Properties
		
		[Searchable]
		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("slug")]
		public string Slug { get; set; }
		
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
		
		[BsonElement("default_language")]
		public string DefaultLanguage { get; set; }

		[BsonElement("mail_providers")]
		public BsonDocument[] MailProviders { get; set; }
		
		[BsonElement("user_activation")]
		public string UserActivation { get; set; }
		
		[BsonElement("code_policy")]
		public string CodePolicy { get; set; }

		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}