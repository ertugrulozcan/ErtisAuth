using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.GeoLocation
{
	public class Coordinate
	{
		#region Properties

		[JsonProperty("latitude")]
		[JsonPropertyName("latitude")]
		public double? Latitude { get; set; }
		
		[JsonProperty("longitude")]
		[JsonPropertyName("longitude")]
		public double? Longitude { get; set; }

		#endregion
	}
}