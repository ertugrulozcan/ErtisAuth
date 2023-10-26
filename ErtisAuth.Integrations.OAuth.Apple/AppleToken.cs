using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public class AppleToken : IProviderToken
{
	#region Properties
		
	[JsonProperty("accessToken")]
	public string? AccessToken { get; set; }
	
	[JsonProperty("idToken")]
	public string? IdToken { get; set; }
	
	[JsonProperty("code")]
	public string? Code { get; set; }
	
	[JsonIgnore]
	public long ExpiresIn { get; set; }
		
	#endregion
}