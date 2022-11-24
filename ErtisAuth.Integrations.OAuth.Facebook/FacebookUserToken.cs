using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook
{
	public class FacebookUserToken : IProviderUser, IProviderToken
	{
		#region Properties
		
		[JsonProperty("id")]
		public string Id { get; set; }
		
		[JsonProperty("first_name")]
		public string FirstName { get; set; }
		
		[JsonProperty("last_name")]
		public string LastName { get; set; }
		
		[JsonProperty("email")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("accessToken")]
		public string AccessToken { get; set; }
		
		[JsonProperty("signedRequest")]
		public string SignedRequest { get; set; }
		
		[JsonProperty("expiresIn")]
		public long ExpiresIn { get; set; }
		
		[JsonProperty("data_access_expiration_time")]
		public long DataAccessExpirationTime { get; set; }
		
		[JsonProperty("picture")]
		public FacebookImageData Picture { get; set; }

		#endregion
	}
}