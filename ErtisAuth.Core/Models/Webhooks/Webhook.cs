using System;
using System.Linq;
using System.Text.Json.Serialization;
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
		[JsonPropertyName("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		[JsonPropertyName("description")]
		public string Description { get; set; }
		
		[JsonProperty("event")]
		[JsonPropertyName("event")]
		public string Event { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
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
		[JsonPropertyName("status")]
		[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
		public WebhookStatus? Status { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public bool IsActive => this.Status == WebhookStatus.Active;

		[JsonProperty("request")]
		[JsonPropertyName("request")]
		public WebhookRequest Request { get; set; }
		
		[JsonProperty("try_count")]
		[JsonPropertyName("try_count")]
		public int TryCount { get; set; }
		
		[JsonProperty("sys")]
		[JsonPropertyName("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}