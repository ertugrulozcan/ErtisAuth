using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks
{
	public class WebhookRequest
	{
		#region Properties

		[JsonProperty("method")]
		[JsonPropertyName("method")]
		public string Method { get; set; }
		
		[JsonProperty("url")]
		[JsonPropertyName("url")]
		public string Url { get; set; }
		
		[JsonProperty("headers")]
		[JsonPropertyName("headers")]
		public Dictionary<string, object> Headers { get; set; }

		[JsonProperty("body")]
		[JsonPropertyName("body")]
		public dynamic Body { get; set; }
		
		#endregion
	}
}