using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request
{
	public class GenerateTokenFormModel
	{
		#region Properties

		[JsonProperty("username")]
		public string Username { get; set; }
		
		[JsonProperty("password")]
		public string Password { get; set; }

		#endregion
	}
}