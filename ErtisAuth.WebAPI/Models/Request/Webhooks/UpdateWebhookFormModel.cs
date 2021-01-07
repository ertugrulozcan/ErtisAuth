using System.Collections.Generic;
using ErtisAuth.Core.Models.Webhooks;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Webhooks
{
	public class UpdateWebhookFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("event")]
		public string Event { get; set; }
		
		[JsonProperty("status")]
		public string Status { get; set; }
		
		[JsonProperty("requests")]
		public IEnumerable<WebhookRequest> RequestList { get; set; }
		
		[JsonProperty("try_count")]
		public int TryCount { get; set; }
		
		#endregion
	}
}