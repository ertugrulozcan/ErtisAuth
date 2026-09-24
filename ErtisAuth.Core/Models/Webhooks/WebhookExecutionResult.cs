using System.Text.Json.Serialization;
using Ertis.Core.Models.Response;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks;

public class WebhookExecutionResult
{
	#region Properties
	
	[JsonProperty("webhook_id")]
	[JsonPropertyName("webhook_id")]
	[BsonElement("webhook_id")]
	public string? WebhookId { get; set; }
	
	[JsonProperty("isSuccess")]
	[JsonPropertyName("isSuccess")]
	[BsonElement("isSuccess")]
	public bool IsSuccess { get; set; }
	
	[JsonProperty("statusCode")]
	[JsonPropertyName("statusCode")]
	[BsonElement("statusCode")]
	public int? StatusCode { get; set; }
	
	[JsonProperty("tryIndex")]
	[JsonPropertyName("tryIndex")]
	[BsonElement("tryIndex")]
	public int TryIndex { get; set; }
	
	[JsonProperty("exception")]
	[JsonPropertyName("exception")]
	[BsonElement("exception")]
	public Exception? Exception { get; set; }
	
	[JsonProperty("request")]
	[JsonPropertyName("request")]
	[BsonElement("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonProperty("response")]
	[JsonPropertyName("response")]
	[BsonElement("response")]
	public IResponseResult? Response { get; set; }
	
	#endregion
}