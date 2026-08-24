using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks;

public class WebhookRequest
{
	#region Properties
	
	[JsonProperty("method")]
	[JsonPropertyName("method")]
	public required string Method { get; set; }
	
	[JsonProperty("url")]
	[JsonPropertyName("url")]
	public required string Url { get; set; }
	
	[JsonProperty("headers")]
	[JsonPropertyName("headers")]
	public Dictionary<string, object>? Headers { get; set; }
	
	[JsonProperty("body")]
	[JsonPropertyName("body")]
	public dynamic? Body { get; set; }
	
	#endregion
}