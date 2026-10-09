using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.GeoLocation
{
	public class GeoLocationInfo
	{
		#region Properties

		[JsonProperty("city")]
		public string City { get; set; }

		[JsonProperty("country")]
		public string Country { get; set; }
		
		[JsonProperty("country_code")]
		public string CountryCode { get; set; }
		
		[JsonProperty("postal_code")]
		public string PostalCode { get; set; }
		
		[JsonProperty("location")]
		public Coordinate Location { get; set; }

		[JsonProperty("isp")]
		public string Isp { get; set; }
		
		[JsonProperty("isp_domain")]
		public string IspDomain { get; set; }
		
		#endregion
	}
}