using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class ClientInfo
{
	#region Properties
	
	[JsonProperty("ip_address")]
	[JsonPropertyName("ip_address")]
	public string? IPAddress { get; set; }
	
	[JsonProperty("user_agent")]
	[JsonPropertyName("user_agent")]
	public string? UserAgent { get; set; }
	
	#endregion
}