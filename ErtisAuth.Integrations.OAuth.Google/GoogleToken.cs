using System.Text.Json.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using JsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Integrations.OAuth.Google;

public class GoogleToken : IProviderToken
{
	#region Properties
	
	[JsonProperty("idToken")]
	[JsonPropertyName("idToken")]
	public string? AccessToken { get; set; }
	
	[JsonProperty("clientId")]
	[JsonPropertyName("clientId")]
	public string? ClientId { get; set; }
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	public long ExpiresIn { get; set; }
	
	#endregion
}