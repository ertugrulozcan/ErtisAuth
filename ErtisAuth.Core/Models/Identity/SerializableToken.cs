using System.Text.Json.Serialization;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

internal sealed class SerializableToken
{
	#region Properties
	
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; set; }
	
	[JsonPropertyName("expires_in")]
	public int ExpiresInTimeStamp { get; set; }
	
	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; set; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresInTimeStamp { get; set; }
	
	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }
	
	#endregion
}