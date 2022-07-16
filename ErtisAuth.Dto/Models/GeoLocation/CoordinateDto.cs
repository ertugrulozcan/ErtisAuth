using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.GeoLocation
{
	public class CoordinateDto
	{
		#region Properties

		[BsonElement("latitude")]
		public double? Latitude { get; set; }
		
		[BsonElement("longitude")]
		public double? Longitude { get; set; }

		#endregion
	}
}