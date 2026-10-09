using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftToken : IProviderToken
{
	#region Properties
	
	[JsonPropertyName("authority")]
	public string? Authority { get; set; }
	
	[JsonPropertyName("uniqueId")]
	public string? UniqueId { get; set; }
	
	[JsonPropertyName("tenantId")]
	public string? TenantId { get; set; }
	
	[JsonPropertyName("scopes")]
	public string[]? Scopes { get; set; }
	
	[JsonPropertyName("account")]
	public MicrosoftAccount? Account { get; set; }
	
	[JsonPropertyName("idToken")]
	public string? IdToken { get; set; }
	
	[JsonPropertyName("idTokenClaims")]
	public JwtTokenClaims? IdTokenClaims { get; set; }
	
	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonPropertyName("fromCache")]
	public bool FromCache { get; set; }
	
	[JsonPropertyName("expiresOn")]
	public DateTime? ExpiresOn { get; set; }
	
	[JsonPropertyName("correlationId")]
	public string? CorrelationId { get; set; }
	
	[JsonPropertyName("requestId")]
	public string? RequestId { get; set; }
	
	[JsonPropertyName("extExpiresOn")]
	public DateTime? ExtExpiresOn { get; set; }
	
	[JsonPropertyName("familyId")]
	public string? FamilyId { get; set; }
	
	[JsonPropertyName("tokenType")]
	public string? TokenType { get; set; }
	
	[JsonPropertyName("state")]
	public string? State { get; set; }
	
	[JsonPropertyName("cloudGraphHostName")]
	public string? CloudGraphHostName { get; set; }
	
	[JsonPropertyName("msGraphHost")]
	public string? MsGraphHost { get; set; }
	
	[JsonPropertyName("fromNativeBroker")]
	public bool FromNativeBroker { get; set; }
	
	[JsonIgnore]
	public long ExpiresIn { get; set; }
	
	#endregion
}