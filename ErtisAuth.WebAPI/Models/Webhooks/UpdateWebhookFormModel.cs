using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Webhooks;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Webhooks;

public class UpdateWebhookFormModel
{
	#region Properties
	
	[JsonProperty("name")]
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonProperty("description")]
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonProperty("event")]
	[JsonPropertyName("event")]
	public string? Event { get; set; }
	
	[JsonProperty("status")]
	[JsonPropertyName("status")]
	public string? Status { get; set; }
	
	[JsonProperty("request")]
	[JsonPropertyName("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonProperty("try_count")]
	[JsonPropertyName("try_count")]
	public int TryCount { get; set; }
	
	#endregion
}