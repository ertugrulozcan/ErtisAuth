using System;
using Ertis.Core.Models.Response;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks
{
	public class WebhookExecutionResult
	{
		#region Properties

		[JsonProperty("webhook_id")]
		public string WebhookId { get; set; }
		
		[JsonProperty("isSuccess")]
		public bool IsSuccess { get; set; }
		
		[JsonProperty("statusCode")]
		public int? StatusCode { get; set; }
		
		[JsonProperty("tryIndex")]
		public int TryIndex { get; set; }
		
		[JsonProperty("exception")]
		public Exception Exception { get; set; }
		
		[JsonProperty("request")]
		public WebhookRequest Request { get; set; }
		
		[JsonProperty("response")]
		public IResponseResult Response { get; set; }

		#endregion
	}
}