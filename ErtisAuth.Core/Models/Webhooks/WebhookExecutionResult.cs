using System.Text.Json.Serialization;
using Ertis.Core.Models;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Core.Models.Webhooks;

public class WebhookExecutionResult
{
	#region Properties
	
	[JsonPropertyName("webhook_id")]
	[BsonElement("webhook_id")]
	public string? WebhookId { get; set; }
	
	[JsonPropertyName("isSuccess")]
	[BsonElement("isSuccess")]
	public bool IsSuccess { get; set; }
	
	[JsonPropertyName("statusCode")]
	[BsonElement("statusCode")]
	public int? StatusCode { get; set; }
	
	[JsonPropertyName("tryIndex")]
	[BsonElement("tryIndex")]
	public int TryIndex { get; set; }
	
	[JsonPropertyName("exception")]
	[BsonElement("exception")]
	public Exception? Exception { get; set; }
	
	[JsonPropertyName("request")]
	[BsonElement("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonPropertyName("response")]
	[BsonElement("response")]
	public IResponseResult? Response { get; set; }
	
	#endregion
}