using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftAccount
{
	#region Properties
	
	[JsonProperty("homeAccountId")]
	[JsonPropertyName("homeAccountId")]
	public string? HomeAccountId { get; set; }
	
	[JsonProperty("environment")]
	[JsonPropertyName("environment")]
	public string? Environment { get; set; }
	
	[JsonProperty("tenantId")]
	[JsonPropertyName("tenantId")]
	public string? TenantId { get; set; }
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonProperty("localAccountId")]
	[JsonPropertyName("localAccountId")]
	public string? LocalAccountId { get; set; }
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("idTokenClaims")]
	[JsonPropertyName("idTokenClaims")]
	public JwtTokenClaims? IdTokenClaims { get; set; }
	
	#endregion
}