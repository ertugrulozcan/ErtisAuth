using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Memberships
{
	public class MembershipDto : EntityBase
	{
		#region Properties
		
		[BsonElement("name")]
		public string Name { get; set; }
		
		#endregion
	}
}