using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Google
{
	public class GoogleUser : IProviderUser
	{
		#region Properties
		
		[JsonProperty("id")]
		public string Id { get; set; }
		
		[JsonProperty("first_name")]
		public string FirstName { get; set; }
		
		[JsonProperty("last_name")]
		public string LastName { get; set; }
		
		[JsonProperty("name")]
		public string FullName { get; set; }
		
		[JsonProperty("email")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("prn")]
        public string Prn { get; set; }

        [JsonProperty("hd")]
        public string HostedDomain { get; set; }

        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonProperty("picture")]
        public string Picture { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }
		
		#endregion
	}
}