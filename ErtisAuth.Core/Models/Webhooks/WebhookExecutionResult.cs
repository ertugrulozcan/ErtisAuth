using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Core.Models.Webhooks;

/// <summary>
/// The payload of the WebhookRequestSent / WebhookRequestFailed events (stored and readable with events.read):
/// plain serializable data only, an exception or a response object can not be serialized as it is.
/// </summary>
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
	public WebhookExecutionError? Exception { get; set; }
	
	[JsonPropertyName("request")]
	[BsonElement("request")]
	public WebhookRequest? Request { get; set; }
	
	[JsonPropertyName("response")]
	[BsonElement("response")]
	public WebhookResponse? Response { get; set; }
	
	#endregion
}

/// <summary>
/// The exception of a request which could not be sent (e.g. connection refused, timeout); the stack trace is not exposed
/// </summary>
public class WebhookExecutionError
{
	#region Properties
	
	[JsonPropertyName("type")]
	[BsonElement("type")]
	public required string Type { get; init; }
	
	[JsonPropertyName("message")]
	[BsonElement("message")]
	public required string Message { get; init; }
	
	#endregion
	
	#region Methods
	
	public static WebhookExecutionError FromException(Exception exception)
	{
		return new WebhookExecutionError
		{
			Type = exception.GetType().Name,
			Message = exception.Message
		};
	}
	
	#endregion
}

/// <summary>
/// The response of the receiver
/// </summary>
public class WebhookResponse
{
	#region Properties
	
	[JsonPropertyName("isSuccess")]
	[BsonElement("isSuccess")]
	public bool IsSuccess { get; init; }
	
	[JsonPropertyName("statusCode")]
	[BsonElement("statusCode")]
	public int? StatusCode { get; init; }
	
	[JsonPropertyName("body")]
	[BsonElement("body")]
	public string? Body { get; init; }
	
	#endregion
}
