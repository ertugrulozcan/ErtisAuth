using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleBearerToken
{
	#region Properties
	
	[JsonPropertyName("access_token")]
	public string? AccessToken { get; set; }
	
	[JsonPropertyName("token_type")]
	public string? TokenType { get; set; }
	
	[JsonPropertyName("expires_in")]
	public long ExpiresIn { get; set; }
	
	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; set; }
	
	[JsonPropertyName("id_token")]
	public string? IdToken { get; set; }
	
	#endregion
}