using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.UserTypes
{
	public class UserTypeDto : EntityBase, IHasMembership, IHasSysDto
	{
		#region Properties

		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("description")]
		public string Description { get; set; }
		
		[BsonElement("slug")]
		public string Slug { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("schema")]
		public SchemaPropertyDto Schema { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}