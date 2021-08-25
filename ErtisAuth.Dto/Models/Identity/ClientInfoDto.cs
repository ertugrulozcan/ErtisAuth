using ErtisAuth.Core.Models.GeoLocation;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity
{
	public class ClientInfoDto
	{
		#region Properties

		[BsonElement("ip_address")]
		public string IPAddress { get; set; }
		
		[BsonElement("user_agent")]
		public string UserAgent { get; set; }
		
		[BsonElement("geo_location")]
		public GeoLocationInfo GeoLocation { get; set; }
		
		#endregion
	}
}