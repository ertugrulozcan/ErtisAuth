using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Facebook;

public class FacebookUserToken : IProviderUser, IProviderToken
{
	#region Properties
	
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	
	[JsonPropertyName("first_name")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("last_name")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("email")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonPropertyName("signedRequest")]
	public string? SignedRequest { get; set; }
	
	[JsonPropertyName("expiresIn")]
	public long ExpiresIn { get; set; }
	
	[JsonPropertyName("data_access_expiration_time")]
	public long DataAccessExpirationTime { get; set; }
	
	[JsonPropertyName("picture")]
	public FacebookImageData? Picture { get; set; }
	
	#endregion
}