using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.GeoLocation
{
	public class GeoLocationInfo
	{
		#region Properties

		[JsonProperty("city")]
		[JsonPropertyName("city")]
		public string City { get; set; }

		[JsonProperty("country")]
		[JsonPropertyName("country")]
		public string Country { get; set; }
		
		[JsonProperty("country_code")]
		[JsonPropertyName("country_code")]
		public string CountryCode { get; set; }
		
		[JsonProperty("postal_code")]
		[JsonPropertyName("postal_code")]
		public string PostalCode { get; set; }
		
		[JsonProperty("location")]
		[JsonPropertyName("location")]
		public Coordinate Location { get; set; }

		[JsonProperty("isp")]
		[JsonPropertyName("isp")]
		public string Isp { get; set; }
		
		[JsonProperty("isp_domain")]
		[JsonPropertyName("isp_domain")]
		public string IspDomain { get; set; }
		
		#endregion
	}
}