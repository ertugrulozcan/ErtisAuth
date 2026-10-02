using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Tokens;

public class GenerateTokenFormModel
{
	#region Properties
	
	[JsonPropertyName("username")]
	public string? Username { get; set; }
	
	[JsonPropertyName("password")]
	public string? Password { get; set; }
	
	[JsonPropertyName("scopes")]
	public string[]? Scopes { get; set; }
	
	#endregion
}