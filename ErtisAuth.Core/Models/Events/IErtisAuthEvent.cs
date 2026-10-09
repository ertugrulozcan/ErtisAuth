using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Events
{
	public interface IErtisAuthEvent
	{
		#region Properties

		[JsonProperty("utilizer_id")]
		string UtilizerId { get; set; }
		
		[JsonProperty("membership_id")]
		string MembershipId { get; set; }

		[JsonProperty("document")]
		dynamic Document { get; set; }
		
		[JsonProperty("prior")]
		dynamic Prior { get; set; }
		
		[JsonProperty("event_time")]
		DateTime EventTime { get; set; }
		
		[JsonProperty("is_custom_event")]
		bool IsCustomEvent { get; }
		
		#endregion
	}
}