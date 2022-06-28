using System;
using System.Collections.Generic;
using System.Linq;
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
		
		[JsonProperty("body_type")]
		public string BodyType { get; set; }

		[JsonIgnore]
		public WebhookRequestBodyType BodyTypeEnum
		{
			get
			{
				if (string.IsNullOrEmpty(this.BodyType))
				{
					return WebhookRequestBodyType.None;
				}
				
				if (Enum.GetNames(typeof(WebhookRequestBodyType)).Any(x => x == this.BodyType))
				{
					return (WebhookRequestBodyType) Enum.Parse(typeof(WebhookRequestBodyType), this.BodyType);
				}
				else
				{
					return WebhookRequestBodyType.None;
				}
			}
		}
		
		#endregion
	}
}