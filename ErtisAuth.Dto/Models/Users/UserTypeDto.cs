using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Users
{
    public class UserTypeDto
    {
        #region Properties

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("name")] 
        public string Name { get; set; }
		
        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("type")]
        public string Type => "object";
		
        [BsonElement("properties")]
        public BsonDocument Properties { get; set; }
		
        [BsonElement("required")]
        public string[] RequiredFields { get; set; }

        #endregion
    }
}