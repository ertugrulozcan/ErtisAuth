using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Applications
{
	public class CreateApplicationFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("slug")]
		public string Slug { get; set; }

		[JsonProperty("role")]
		public string Role { get; set; }
		
		#endregion
	}
}