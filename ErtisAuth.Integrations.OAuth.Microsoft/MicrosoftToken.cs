using System;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Microsoft
{
	public class MicrosoftToken : IProviderToken
	{
		#region Properties
		
		[JsonProperty("authority")]
		public string Authority { get; set; }
		
		[JsonProperty("uniqueId")]
		public string UniqueId { get; set; }
		
		[JsonProperty("tenantId")]
		public string TenantId { get; set; }
		
		[JsonProperty("scopes")]
		public string[] Scopes { get; set; }
		
		[JsonProperty("account")]
		public MicrosoftAccount Account { get; set; }
		
		[JsonProperty("idToken")]
		public string IdToken { get; set; }
		
		[JsonProperty("idTokenClaims")]
		public JwtTokenClaims IdTokenClaims { get; set; }
		
		[JsonProperty("accessToken")]
		public string AccessToken { get; set; }
		
		[JsonProperty("fromCache")]
		public bool FromCache { get; set; }
		
		[JsonProperty("expiresOn")]
		public DateTime? ExpiresOn { get; set; }
		
		[JsonProperty("correlationId")]
		public string CorrelationId { get; set; }
		
		[JsonProperty("requestId")]
		public string RequestId { get; set; }
		
		[JsonProperty("extExpiresOn")]
		public DateTime? ExtExpiresOn { get; set; }
		
		[JsonProperty("familyId")]
		public string FamilyId { get; set; }
		
		[JsonProperty("tokenType")]
		public string TokenType { get; set; }
		
		[JsonProperty("state")]
		public string State { get; set; }
		
		[JsonProperty("cloudGraphHostName")]
		public string CloudGraphHostName { get; set; }
		
		[JsonProperty("msGraphHost")]
		public string MsGraphHost { get; set; }
		
		[JsonProperty("fromNativeBroker")]
		public bool FromNativeBroker { get; set; }
		
		[JsonIgnore]
		public long ExpiresIn { get; set; }
		
		#endregion
	}
}