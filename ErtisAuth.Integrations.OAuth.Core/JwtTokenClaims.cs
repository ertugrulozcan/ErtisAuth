using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Core;

public class JwtTokenClaims
{
	#region Properties
	
	[JsonPropertyName("aud")]
	public string? Aud { get; set; }
	
	[JsonPropertyName("iss")]
	public string? Iss { get; set; }
	
	[JsonPropertyName("iat")]
	public string? Iat { get; set; }
	
	[JsonPropertyName("nbf")]
	public string? Nbf { get; set; }
	
	[JsonPropertyName("exp")]
	public string? Exp { get; set; }
	
	[JsonPropertyName("aio")]
	public string? Aio { get; set; }
	
	[JsonPropertyName("idp")]
	public string? Idp { get; set; }
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("nonce")]
	public string? Nonce { get; set; }
	
	[JsonPropertyName("oid")]
	public string? Oid { get; set; }
	
	[JsonPropertyName("preferred_username")]
	public string? PreferredUsername { get; set; }
	
	[JsonPropertyName("rh")]
	public string? Rh { get; set; }
	
	[JsonPropertyName("sub")]
	public string? Sub { get; set; }
	
	[JsonPropertyName("tid")]
	public string? Tid { get; set; }
	
	[JsonPropertyName("uti")]
	public string? Uti { get; set; }
	
	[JsonPropertyName("ver")]
	public string? Version { get; set; }
		
	#endregion
}