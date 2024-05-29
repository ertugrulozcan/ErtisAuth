using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Models.GeoLocation
{
	public class Ip2LocationLookupResponse
	{
		#region Properties

		[JsonProperty("country_code")]
		[JsonPropertyName("country_code")]
		public string CountryCode { get; set; }
		
		[JsonProperty("country_name")]
		[JsonPropertyName("country_name")]
		public string CountryName { get; set; }
		
		[JsonProperty("region_name")]
		[JsonPropertyName("region_name")]
		public string RegionName { get; set; }
		
		[JsonProperty("city_name")]
		[JsonPropertyName("city_name")]
		public string CityName { get; set; }
		
		[JsonProperty("latitude")]
		[JsonPropertyName("latitude")]
		public string Latitude { get; set; }
		
		[JsonProperty("longitude")]
		[JsonPropertyName("longitude")]
		public string Longitude { get; set; }
		
		[JsonProperty("zip_code")]
		[JsonPropertyName("zip_code")]
		public string ZipCode { get; set; }
		
		[JsonProperty("time_zone")]
		[JsonPropertyName("time_zone")]
		public string TimeZone { get; set; }
		
		[JsonProperty("isp")]
		[JsonPropertyName("isp")]
		public string Isp { get; set; }
		
		[JsonProperty("domain")]
		[JsonPropertyName("domain")]
		public string Domain { get; set; }
		
		[JsonProperty("net_speed")]
		[JsonPropertyName("net_speed")]
		public string NetSpeed { get; set; }
		
		[JsonProperty("idd_code")]
		[JsonPropertyName("idd_code")]
		public string IddCode { get; set; }
		
		[JsonProperty("area_code")]
		[JsonPropertyName("area_code")]
		public string AreaCode { get; set; }
		
		[JsonProperty("weather_station_code")]
		[JsonPropertyName("weather_station_code")]
		public string WeatherStationCode { get; set; }
		
		[JsonProperty("weather_station_name")]
		[JsonPropertyName("weather_station_name")]
		public string WeatherStationName { get; set; }
		
		[JsonProperty("mcc")]
		[JsonPropertyName("mcc")]
		public string Mcc { get; set; }
		
		[JsonProperty("mnc")]
		[JsonPropertyName("mnc")]
		public string Mnc { get; set; }
		
		[JsonProperty("mobile_brand")]
		[JsonPropertyName("mobile_brand")]
		public string MobileBrand { get; set; }
		
		[JsonProperty("elevation")]
		[JsonPropertyName("elevation")]
		public string Elevation { get; set; }
		
		[JsonProperty("usage_type")]
		[JsonPropertyName("usage_type")]
		public string UsageType { get; set; }
		
		[JsonProperty("address_type")]
		[JsonPropertyName("address_type")]
		public string AddressType { get; set; }
		
		[JsonProperty("category")]
		[JsonPropertyName("category")]
		public string Category { get; set; }
		
		[JsonProperty("category_name")]
		[JsonPropertyName("category_name")]
		public string CategoryName { get; set; }
		
		[JsonProperty("credits_consumed")]
		[JsonPropertyName("credits_consumed")]
		public int CreditsConsumed { get; set; }

		#endregion
	}
}