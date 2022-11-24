using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Providers
{
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
		
		[BsonElement("appId")]
		public string AppId { get; set; }
		
		[BsonElement("isActive")]
		public bool? IsActive { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}