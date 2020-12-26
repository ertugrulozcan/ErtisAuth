using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Tokens
{
	public class VerifyTokenFormModel
	{
		#region Properties

		[JsonProperty("token")]
		public string Token { get; set; }
		
		#endregion
	}
}