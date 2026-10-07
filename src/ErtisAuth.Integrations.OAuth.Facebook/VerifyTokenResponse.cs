using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Facebook;

public class VerifyTokenResponse
{
	#region Properties
	
	[JsonPropertyName("data")]
	public VerifyTokenResponseData? Data { get; set; }
	
	#endregion
}

public class VerifyTokenResponseData
{
	#region Properties
	
	[JsonPropertyName("app_id")]
	public string? AppId { get; set; }
	
	[JsonPropertyName("user_id")]
	public string? UserId { get; set; }
	
	[JsonPropertyName("type")]
	public string? Type { get; set; }
	
	[JsonPropertyName("application")]
	public string? Application { get; set; }
	
	[JsonPropertyName("data_access_expires_at")]
	public long DataAccessExpiresAt { get; set; }
	
	[JsonPropertyName("expires_at")]
	public long ExpiresAt { get; set; }
	
	[JsonPropertyName("is_valid")]
	public bool IsValid { get; set; }
	
	[JsonPropertyName("scopes")]
	public string[]? Scopes { get; set; }
	
	#endregion
}