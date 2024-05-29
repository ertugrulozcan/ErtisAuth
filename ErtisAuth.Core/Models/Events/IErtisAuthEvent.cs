using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Events
{
	public interface IErtisAuthEvent
	{
		#region Properties

		[JsonProperty("utilizer_id")]
		[JsonPropertyName("utilizer_id")]
		string UtilizerId { get; set; }
		
		[JsonProperty("membership_id")]
		[JsonPropertyName("membership_id")]
		string MembershipId { get; set; }

		[JsonProperty("document")]
		[JsonPropertyName("document")]
		dynamic Document { get; set; }
		
		[JsonProperty("prior")]
		[JsonPropertyName("prior")]
		dynamic Prior { get; set; }
		
		[JsonProperty("event_time")]
		[JsonPropertyName("event_time")]
		DateTime EventTime { get; set; }
		
		[JsonProperty("is_custom_event")]
		[JsonPropertyName("is_custom_event")]
		bool IsCustomEvent { get; }
		
		#endregion
	}
}