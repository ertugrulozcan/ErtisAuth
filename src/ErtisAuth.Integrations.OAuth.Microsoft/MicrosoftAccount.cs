using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Microsoft;

public class MicrosoftAccount
{
	#region Properties
	
	[JsonPropertyName("homeAccountId")]
	public string? HomeAccountId { get; set; }
	
	[JsonPropertyName("environment")]
	public string? Environment { get; set; }
	
	[JsonPropertyName("tenantId")]
	public string? TenantId { get; set; }
	
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonPropertyName("localAccountId")]
	public string? LocalAccountId { get; set; }
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("idTokenClaims")]
	public JwtTokenClaims? IdTokenClaims { get; set; }
	
	#endregion
}