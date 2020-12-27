using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.UserTypes
{
	public class SchemaPropertyDto
	{
		#region Properties

		[BsonElement("name")]
		public string Name { get; set; }
		
		[BsonElement("type")]
		public string Type { get; set; }

		[BsonElement("default_value")]
		public object DefaultValue { get; set; }
		
		[BsonElement("is_required")]
		public bool IsRequired { get; set; }
		
		[BsonElement("properties")]
		public IEnumerable<SchemaPropertyDto> Properties { get; set; }
		
		#endregion
	}
}