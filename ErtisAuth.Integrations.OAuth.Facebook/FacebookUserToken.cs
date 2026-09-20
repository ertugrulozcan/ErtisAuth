using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook;

public class FacebookUserToken : IProviderUser, IProviderToken
{
	#region Properties
	
	[JsonProperty("id")]
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonProperty("first_name")]
	[JsonPropertyName("first_name")]
	public string? FirstName { get; set; }
	
	[JsonProperty("last_name")]
	[JsonPropertyName("last_name")]
	public string? LastName { get; set; }
	
	[JsonProperty("email")]
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("accessToken")]
	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonProperty("signedRequest")]
	[JsonPropertyName("signedRequest")]
	public string? SignedRequest { get; set; }
	
	[JsonProperty("expiresIn")]
	[JsonPropertyName("expiresIn")]
	public long ExpiresIn { get; set; }
	
	[JsonProperty("data_access_expiration_time")]
	[JsonPropertyName("data_access_expiration_time")]
	public long DataAccessExpirationTime { get; set; }
	
	[JsonProperty("picture")]
	[JsonPropertyName("picture")]
	public FacebookImageData? Picture { get; set; }
	
	#endregion
}