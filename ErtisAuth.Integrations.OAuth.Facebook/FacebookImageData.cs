using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook
{
	public class FacebookImageData
	{
		#region Properties

		[JsonProperty("data")]
		public FacebookImage Data { get; set; }

		#endregion
	}
}