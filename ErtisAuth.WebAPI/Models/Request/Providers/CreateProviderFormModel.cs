using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Providers
{
	public class CreateProviderFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		#endregion
	}
}