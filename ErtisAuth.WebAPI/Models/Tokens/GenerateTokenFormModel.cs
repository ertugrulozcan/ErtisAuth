using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Tokens;

public class GenerateTokenFormModel
{
	#region Properties
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonProperty("password")]
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonProperty("scopes")]
	[JsonPropertyName("scopes")]
	public string[]? Scopes { get; set; }
	
	#endregion
}