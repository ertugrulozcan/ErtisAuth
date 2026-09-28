using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Setup;

public class SetupMembershipModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("slug")]
	[JsonPropertyName("slug")]
	public string? Slug { get; set; }
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonProperty("refresh_token_expires_in")]
	[JsonPropertyName("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonProperty("hash_algorithm")]
	[JsonPropertyName("hash_algorithm")]
	public string? HashAlgorithm { get; set; }
	
	[JsonProperty("encoding")]
	[JsonPropertyName("encoding")]
	public string? DefaultEncoding { get; set; }
	
	/// <summary>
	/// Generated when omitted.
	/// </summary>
	[JsonProperty("secret_key")]
	[JsonPropertyName("secret_key")]
	public string? SecretKey { get; set; }
	
	#endregion
}