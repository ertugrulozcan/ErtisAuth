using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftToken : IProviderToken
{
	#region Properties
	
	[JsonProperty("authority")]
	[JsonPropertyName("authority")]
	public string? Authority { get; set; }
	
	[JsonProperty("uniqueId")]
	[JsonPropertyName("uniqueId")]
	public string? UniqueId { get; set; }
	
	[JsonProperty("tenantId")]
	[JsonPropertyName("tenantId")]
	public string? TenantId { get; set; }
	
	[JsonProperty("scopes")]
	[JsonPropertyName("scopes")]
	public string[]? Scopes { get; set; }
	
	[JsonProperty("account")]
	[JsonPropertyName("account")]
	public MicrosoftAccount? Account { get; set; }
	
	[JsonProperty("idToken")]
	[JsonPropertyName("idToken")]
	public string? IdToken { get; set; }
	
	[JsonProperty("idTokenClaims")]
	[JsonPropertyName("idTokenClaims")]
	public JwtTokenClaims? IdTokenClaims { get; set; }
	
	[JsonProperty("accessToken")]
	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonProperty("fromCache")]
	[JsonPropertyName("fromCache")]
	public bool FromCache { get; set; }
	
	[JsonProperty("expiresOn")]
	[JsonPropertyName("expiresOn")]
	public DateTime? ExpiresOn { get; set; }
	
	[JsonProperty("correlationId")]
	[JsonPropertyName("correlationId")]
	public string? CorrelationId { get; set; }
	
	[JsonProperty("requestId")]
	[JsonPropertyName("requestId")]
	public string? RequestId { get; set; }
	
	[JsonProperty("extExpiresOn")]
	[JsonPropertyName("extExpiresOn")]
	public DateTime? ExtExpiresOn { get; set; }
	
	[JsonProperty("familyId")]
	[JsonPropertyName("familyId")]
	public string? FamilyId { get; set; }
	
	[JsonProperty("tokenType")]
	[JsonPropertyName("tokenType")]
	public string? TokenType { get; set; }
	
	[JsonProperty("state")]
	[JsonPropertyName("state")]
	public string? State { get; set; }
	
	[JsonProperty("cloudGraphHostName")]
	[JsonPropertyName("cloudGraphHostName")]
	public string? CloudGraphHostName { get; set; }
	
	[JsonProperty("msGraphHost")]
	[JsonPropertyName("msGraphHost")]
	public string? MsGraphHost { get; set; }
	
	[JsonProperty("fromNativeBroker")]
	[JsonPropertyName("fromNativeBroker")]
	public bool FromNativeBroker { get; set; }
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public long ExpiresIn { get; set; }
	
	#endregion
}