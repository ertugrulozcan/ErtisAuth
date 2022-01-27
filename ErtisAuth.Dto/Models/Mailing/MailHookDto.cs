using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Mailing
{
    public class MailHookDto : EntityBase, IHasMembership, IHasSysDto
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
		
        [BsonElement("mail_template")]
        public string MailTemplate { get; set; }
		
        [BsonElement("sys")]
        public SysModelDto Sys { get; set; }
		
        #endregion
    }
}