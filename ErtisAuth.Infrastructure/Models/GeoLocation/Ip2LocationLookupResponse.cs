using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Models.GeoLocation
{
	public class Ip2LocationLookupResponse
	{
		#region Properties

		[JsonProperty("country_code")]
		public string CountryCode { get; set; }
		
		[JsonProperty("country_name")]
		public string CountryName { get; set; }
		
		[JsonProperty("region_name")]
		public string RegionName { get; set; }
		
		[JsonProperty("city_name")]
		public string CityName { get; set; }
		
		[JsonProperty("latitude")]
		public string Latitude { get; set; }
		
		[JsonProperty("longitude")]
		public string Longitude { get; set; }
		
		[JsonProperty("zip_code")]
		public string ZipCode { get; set; }
		
		[JsonProperty("time_zone")]
		public string TimeZone { get; set; }
		
		[JsonProperty("isp")]
		public string Isp { get; set; }
		
		[JsonProperty("domain")]
		public string Domain { get; set; }
		
		[JsonProperty("net_speed")]
		public string NetSpeed { get; set; }
		
		[JsonProperty("idd_code")]
		public string IddCode { get; set; }
		
		[JsonProperty("area_code")]
		public string AreaCode { get; set; }
		
		[JsonProperty("weather_station_code")]
		public string WeatherStationCode { get; set; }
		
		[JsonProperty("weather_station_name")]
		public string WeatherStationName { get; set; }
		
		[JsonProperty("mcc")]
		public string Mcc { get; set; }
		
		[JsonProperty("mnc")]
		public string Mnc { get; set; }
		
		[JsonProperty("mobile_brand")]
		public string MobileBrand { get; set; }
		
		[JsonProperty("elevation")]
		public string Elevation { get; set; }
		
		[JsonProperty("usage_type")]
		public string UsageType { get; set; }
		
		[JsonProperty("address_type")]
		public string AddressType { get; set; }
		
		[JsonProperty("category")]
		public string Category { get; set; }
		
		[JsonProperty("category_name")]
		public string CategoryName { get; set; }
		
		[JsonProperty("credits_consumed")]
		public int CreditsConsumed { get; set; }

		#endregion
	}
}