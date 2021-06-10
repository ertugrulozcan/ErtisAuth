using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Events
{
	public abstract class ErtisAuthEventBase : ResourceBase, IHasMembership
	{
		#region Properties

		[JsonProperty("utilizer_id")]
		public string UtilizerId { get; set; }
		
		[JsonProperty("membership_id")]
		public string MembershipId { get; set; }

		[JsonProperty("document")]
		public dynamic Document { get; set; }
		
		[JsonProperty("prior")]
		public dynamic Prior { get; set; }
		
		[JsonProperty("event_time")]
		public DateTime EventTime { get; set; }
		
		[JsonProperty("is_custom_event")]
		public abstract bool IsCustomEvent { get; }
		
		#endregion
	}
}