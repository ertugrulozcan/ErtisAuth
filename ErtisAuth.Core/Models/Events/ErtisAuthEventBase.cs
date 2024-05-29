using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Events
{
	public abstract class ErtisAuthEventBase : ResourceBase, IErtisAuthEvent, IHasMembership
	{
		#region Properties

		[JsonProperty("utilizer_id")]
		[JsonPropertyName("utilizer_id")]
		public string UtilizerId { get; set; }
		
		[JsonProperty("membership_id")]
		[JsonPropertyName("membership_id")]
		public string MembershipId { get; set; }

		[JsonProperty("document")]
		[JsonPropertyName("document")]
		public dynamic Document { get; set; }
		
		[JsonProperty("prior")]
		[JsonPropertyName("prior")]
		public dynamic Prior { get; set; }
		
		[JsonProperty("event_time")]
		[JsonPropertyName("event_time")]
		public DateTime EventTime { get; set; }
		
		[JsonProperty("is_custom_event")]
		[JsonPropertyName("is_custom_event")]
		public abstract bool IsCustomEvent { get; }
		
		#endregion
	}
}