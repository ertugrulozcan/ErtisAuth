using ErtisAuth.Core.Models.GeoLocation;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public struct ClientInfo
	{
		#region Properties

		[JsonProperty("ip_address")]
		public string IPAddress { get; set; }
		
		[JsonProperty("user_agent")]
		public string UserAgent { get; set; }
		
		[JsonProperty("geo_location")]
		public GeoLocationInfo GeoLocation { get; set; }
		
		#endregion
	}
}