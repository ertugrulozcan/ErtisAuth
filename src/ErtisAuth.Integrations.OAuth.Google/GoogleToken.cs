using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Integrations.OAuth.Google;

public class GoogleToken : IProviderToken
{
	#region Properties
	
	[JsonPropertyName("idToken")]
	public string? AccessToken { get; set; }
	
	[JsonPropertyName("clientId")]
	public string? ClientId { get; set; }
	
	[JsonIgnore]
	public long ExpiresIn { get; set; }
	
	#endregion
}