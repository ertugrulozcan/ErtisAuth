using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.GeoLocation
{
	public class Coordinate
	{
		#region Properties

		[JsonProperty("latitude")]
		public double? Latitude { get; set; }
		
		[JsonProperty("longitude")]
		public double? Longitude { get; set; }

		#endregion
	}
}