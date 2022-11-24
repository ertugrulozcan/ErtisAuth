using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Google
{
	public class GoogleToken : IProviderToken
	{
		#region Properties
		
		[JsonProperty("idToken")]
		public string AccessToken { get; set; }
		
		[JsonProperty("clientId")]
		public string ClientId { get; set; }
		
		[JsonIgnore]
		public long ExpiresIn { get; set; }
		
		#endregion
	}
}