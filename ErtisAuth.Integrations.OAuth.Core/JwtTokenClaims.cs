using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Core
{
	public class JwtTokenClaims
	{
		#region Properties

		[JsonProperty("aud")]
		[JsonPropertyName("aud")]
		public string Aud { get; set; }
		
		[JsonProperty("iss")]
		[JsonPropertyName("iss")]
		public string Iss { get; set; }
		
		[JsonProperty("iat")]
		[JsonPropertyName("iat")]
		public string Iat { get; set; }
		
		[JsonProperty("nbf")]
		[JsonPropertyName("nbf")]
		public string Nbf { get; set; }
		
		[JsonProperty("exp")]
		[JsonPropertyName("exp")]
		public string Exp { get; set; }
		
		[JsonProperty("aio")]
		[JsonPropertyName("aio")]
		public string Aio { get; set; }
		
		[JsonProperty("idp")]
		[JsonPropertyName("idp")]
		public string Idp { get; set; }
		
		[JsonProperty("name")]
		[JsonPropertyName("name")]
		public string Name { get; set; }
		
		[JsonProperty("nonce")]
		[JsonPropertyName("nonce")]
		public string Nonce { get; set; }
		
		[JsonProperty("oid")]
		[JsonPropertyName("oid")]
		public string Oid { get; set; }
		
		[JsonProperty("preferred_username")]
		[JsonPropertyName("preferred_username")]
		public string PreferredUsername { get; set; }
		
		[JsonProperty("rh")]
		[JsonPropertyName("rh")]
		public string Rh { get; set; }
		
		[JsonProperty("sub")]
		[JsonPropertyName("sub")]
		public string Sub { get; set; }
		
		[JsonProperty("tid")]
		[JsonPropertyName("tid")]
		public string Tid { get; set; }
		
		[JsonProperty("uti")]
		[JsonPropertyName("uti")]
		public string Uti { get; set; }
		
		[JsonProperty("ver")]
		[JsonPropertyName("ver")]
		public string Version { get; set; }
			
		#endregion
	}
}