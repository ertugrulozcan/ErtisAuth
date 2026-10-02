using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Setup;

public class SetupMembershipModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonPropertyName("hash_algorithm")]
	public string? HashAlgorithm { get; set; }
	
	[JsonPropertyName("encoding")]
	public string? DefaultEncoding { get; set; }
	
	/// <summary>
	/// Generated when omitted.
	/// </summary>
	[JsonPropertyName("secret_key")]
	public string? SecretKey { get; set; }
	
	#endregion
}