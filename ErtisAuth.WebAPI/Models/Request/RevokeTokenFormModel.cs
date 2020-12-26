using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request
{
	public class RevokeTokenFormModel
	{
		#region Properties

		[JsonProperty("token")]
		public string Token { get; set; }
		
		#endregion
	}
}