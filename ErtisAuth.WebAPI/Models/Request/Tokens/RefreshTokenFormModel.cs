using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Tokens
{
	public class RefreshTokenFormModel
	{
		#region Properties

		[JsonProperty("token")]
		public string Token { get; set; }
		
		#endregion
	}
}