using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleBearerToken
{
	#region Properties
	
	[JsonProperty("access_token")]
	[JsonPropertyName("access_token")]
	public string? AccessToken { get; set; }
	
	[JsonProperty("token_type")]
	[JsonPropertyName("token_type")]
	public string? TokenType { get; set; }
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	public long ExpiresIn { get; set; }
	
	[JsonProperty("refresh_token")]
	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; set; }
	
	[JsonProperty("id_token")]
	[JsonPropertyName("id_token")]
	public string? IdToken { get; set; }
	
	#endregion
}