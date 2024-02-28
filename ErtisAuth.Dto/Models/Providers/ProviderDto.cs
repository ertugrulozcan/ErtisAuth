using System.Diagnostics.CodeAnalysis;
using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Providers
{
	// ReSharper disable once ClassNeverInstantiated.Global
	// ReSharper disable PropertyCanBeMadeInitOnly.Global
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public class ProviderDto : EntityBase, IHasMembership, IHasSysDto
	{
		#region Properties

		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("description")]
		public string Description { get; set; }
		
		[BsonElement("defaultRole")]
		public string DefaultRole { get; set; }
		
		[BsonElement("defaultUserType")]
		public string DefaultUserType { get; set; }
		
		[BsonElement("appClientId")]
		public string AppClientId { get; set; }
		
		[BsonElement("teamId")]
		public string TeamId { get; set; }
		
		[BsonElement("tenantId")]
		public string TenantId { get; set; }
		
		[BsonElement("privateKey")]
		public string PrivateKey { get; set; }
		
		[BsonElement("privateKeyId")]
		public string PrivateKeyId { get; set; }
		
		[BsonElement("redirectUri")]
		public string RedirectUri { get; set; }
		
		[BsonElement("isActive")]
		public bool? IsActive { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}