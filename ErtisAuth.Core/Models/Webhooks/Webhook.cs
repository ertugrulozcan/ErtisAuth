using System;
using System.Linq;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.Webhooks
{
	public class Webhook : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("event")]
		public string Event { get; set; }

		[JsonIgnore]
		public ErtisAuthEventType? EventType
		{
			get
			{
				if (Enum.GetNames(typeof(ErtisAuthEventType)).Any(x => x == this.Event))
				{
					return (ErtisAuthEventType) Enum.Parse(typeof(ErtisAuthEventType), this.Event);
				}
				else
				{
					return null;
				}
			}
		}

		[JsonProperty("status")]
		[JsonConverter(typeof(StringEnumConverter))]
		public WebhookStatus? Status { get; set; }

		[JsonIgnore]
		public bool IsActive => this.Status == WebhookStatus.Active;

		[JsonProperty("request")]
		public WebhookRequest Request { get; set; }
		
		[JsonProperty("try_count")]
		public int TryCount { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}