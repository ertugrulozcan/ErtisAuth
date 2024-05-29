using System;
using System.Text.Json.Serialization;
using Ertis.Core.Models.Response;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks
{
	public class WebhookExecutionResult
	{
		#region Properties

		[JsonProperty("webhook_id")]
		[JsonPropertyName("webhook_id")]
		public string WebhookId { get; set; }
		
		[JsonProperty("isSuccess")]
		[JsonPropertyName("isSuccess")]
		public bool IsSuccess { get; set; }
		
		[JsonProperty("statusCode")]
		[JsonPropertyName("statusCode")]
		public int? StatusCode { get; set; }
		
		[JsonProperty("tryIndex")]
		[JsonPropertyName("tryIndex")]
		public int TryIndex { get; set; }
		
		[JsonProperty("exception")]
		[JsonPropertyName("exception")]
		public Exception Exception { get; set; }
		
		[JsonProperty("request")]
		[JsonPropertyName("request")]
		public WebhookRequest Request { get; set; }
		
		[JsonProperty("response")]
		[JsonPropertyName("response")]
		public IResponseResult Response { get; set; }

		#endregion
	}
}