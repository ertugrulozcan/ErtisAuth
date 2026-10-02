using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleToken : IProviderToken
{
	#region Properties
	
	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonPropertyName("idToken")]
	public string? IdToken { get; set; }
	
	[JsonPropertyName("code")]
	public string? Code { get; set; }
	
	[JsonIgnore]
	public long ExpiresIn { get; set; }
	
	#endregion
}