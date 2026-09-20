using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleToken : IProviderToken
{
	#region Properties
	
	[JsonProperty("accessToken")]
	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonProperty("idToken")]
	[JsonPropertyName("idToken")]
	public string? IdToken { get; set; }
	
	[JsonProperty("code")]
	[JsonPropertyName("code")]
	public string? Code { get; set; }
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public long ExpiresIn { get; set; }
	
	#endregion
}