using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Tokens
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