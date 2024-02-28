using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Applications
{
	public class UpdateApplicationFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("slug")]
		public string Slug { get; set; }
		
		[JsonProperty("secret")]
		public string Secret { get; set; }
		
		[JsonProperty("role")]
		public string Role { get; set; }

		#endregion
	}
}