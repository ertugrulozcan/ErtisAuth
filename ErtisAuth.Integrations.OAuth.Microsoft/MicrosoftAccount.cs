using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Microsoft
{
	public class MicrosoftAccount
	{
		#region Properties

		[JsonProperty("homeAccountId")]
		public string HomeAccountId { get; set; }
		
		[JsonProperty("environment")]
		public string Environment { get; set; }
		
		[JsonProperty("tenantId")]
		public string TenantId { get; set; }
		
		[JsonProperty("username")]
		public string Username { get; set; }
		
		[JsonProperty("localAccountId")]
		public string LocalAccountId { get; set; }
		
		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("idTokenClaims")]
		public JwtTokenClaims IdTokenClaims { get; set; }

		#endregion
	}
}