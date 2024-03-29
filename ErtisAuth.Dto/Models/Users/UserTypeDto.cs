using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Users
{
    public class UserTypeDto : EntityBase, IHasMembership, IHasSysDto
    {
        #region Properties

        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("slug")]
        public string Slug { get; set; }
        
        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("properties")]
        public BsonDocument Properties { get; set; }
        
        [BsonElement("allowAdditionalProperties")]
        public bool AllowAdditionalProperties { get; init; }
        
        [BsonElement("isAbstract")]
        public bool IsAbstract { get; set; }

        [BsonElement("isSealed")]
        public bool IsSealed { get; set; }
		
        [BsonElement("baseType")]
        public string BaseUserType { get; set; }

        [BsonElement("membership_id")]
        public string MembershipId { get; set; }
		
        [BsonElement("sys")]
        public SysModelDto Sys { get; set; }

        #endregion
    }
}