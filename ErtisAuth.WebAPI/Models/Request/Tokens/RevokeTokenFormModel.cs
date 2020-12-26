using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Tokens
{
	public class RevokeTokenFormModel
	{
		#region Properties

		[JsonProperty("token")]
		public string Token { get; set; }
		
		#endregion
	}
}