using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Webhooks
{
	public class WebhookDto : EntityBase, IHasMembership, IHasSysDto
	{
		#region Properties

		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("description")]
		public string Description { get; set; }
		
		[BsonElement("event")]
		public string Event { get; set; }
		
		[BsonElement("membership_id")]
		public string MembershipId { get; set; }
		
		[BsonElement("status")]
		public string Status { get; set; }
		
		[BsonElement("request")]
		public WebhookRequestDto Request { get; set; }

		[BsonElement("try_count")]
		public int TryCount { get; set; }
		
		[BsonElement("sys")]
		public SysModelDto Sys { get; set; }
		
		#endregion
	}
}