using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Webhooks;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.WebAPI.Models.Webhooks;

public class UpdateWebhookFormModel
{
	#region Properties
	
	[JsonPropertyName("name")]
	public string? Name { get; set; }
	
	[JsonPropertyName("description")]
	public string? Description { get; set; }
	
	[JsonPropertyName("event")]
	public string? Event { get; set; }
	
	[JsonPropertyName("status")]
	public string? Status { get; set; }
	
	[JsonPropertyName("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonPropertyName("try_count")]
	public int TryCount { get; set; }
	
	#endregion
}