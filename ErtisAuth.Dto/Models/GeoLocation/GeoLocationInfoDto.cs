using ErtisAuth.Core.Models.GeoLocation;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.GeoLocation
{
	public class GeoLocationInfoDto
	{
		#region Properties

		[BsonElement("city")]
		public string City { get; set; }

		[BsonElement("country")]
		public string Country { get; set; }
		
		[BsonElement("country_code")]
		public string CountryCode { get; set; }
		
		[BsonElement("postal_code")]
		public string PostalCode { get; set; }
		
		[BsonElement("location")]
		public Coordinate Location { get; set; }

		[BsonElement("isp")]
		public string Isp { get; set; }
		
		[BsonElement("isp_domain")]
		public string IspDomain { get; set; }
		
		#endregion
	}
}