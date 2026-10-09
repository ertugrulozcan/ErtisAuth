using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Core
{
	public class JwtTokenClaims
	{
		#region Properties

		[JsonProperty("aud")]
		public string Aud { get; set; }
		
		[JsonProperty("iss")]
		public string Iss { get; set; }
		
		[JsonProperty("iat")]
		public string Iat { get; set; }
		
		[JsonProperty("nbf")]
		public string Nbf { get; set; }
		
		[JsonProperty("exp")]
		public string Exp { get; set; }
		
		[JsonProperty("aio")]
		public string Aio { get; set; }
		
		[JsonProperty("idp")]
		public string Idp { get; set; }
		
		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("nonce")]
		public string Nonce { get; set; }
		
		[JsonProperty("oid")]
		public string Oid { get; set; }
		
		[JsonProperty("preferred_username")]
		public string PreferredUsername { get; set; }
		
		[JsonProperty("rh")]
		public string Rh { get; set; }
		
		[JsonProperty("sub")]
		public string Sub { get; set; }
		
		[JsonProperty("tid")]
		public string Tid { get; set; }
		
		[JsonProperty("uti")]
		public string Uti { get; set; }
		
		[JsonProperty("ver")]
		public string Version { get; set; }
			
		#endregion
	}
}