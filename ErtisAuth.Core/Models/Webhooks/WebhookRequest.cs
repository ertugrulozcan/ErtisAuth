using System.Collections.Generic;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Webhooks
{
	public class WebhookRequest
	{
		#region Properties

		[JsonProperty("method")]
		public string Method { get; set; }
		
		[JsonProperty("url")]
		public string Url { get; set; }
		
		[JsonProperty("headers")]
		public Dictionary<string, object> Headers { get; set; }

		[JsonProperty("body")]
		public dynamic Body { get; set; }
		
		#endregion
	}
}