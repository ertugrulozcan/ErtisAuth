using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Events
{
	public class ErtisAuthCustomEvent : ErtisAuthEventBase
	{
		#region Properties

		[JsonProperty("event_type")]
		[JsonPropertyName("event_type")]
		public string EventType { get; set; }
		
		[JsonProperty("is_custom_event")] 
		[JsonPropertyName("is_custom_event")] 
		public override bool IsCustomEvent => true;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Default Constructor
		/// </summary>
		public ErtisAuthCustomEvent()
		{
			
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type"></param>
		/// <param name="user"></param>
		/// <param name="document"></param>
		/// <param name="prior"></param>
		public ErtisAuthCustomEvent(string type, User user, dynamic document = null, dynamic prior = null)
		{
			this.EventType = type;
			this.UtilizerId = user.Id;
			this.MembershipId = user.MembershipId;
			this.Document = document;
			this.Prior = prior;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type"></param>
		/// <param name="application"></param>
		/// <param name="document"></param>
		/// <param name="prior"></param>
		public ErtisAuthCustomEvent(string type, Application application, dynamic document = null, dynamic prior = null)
		{
			this.EventType = type;
			this.UtilizerId = application.Id;
			this.MembershipId = application.MembershipId;
			this.Document = document;
			this.Prior = prior;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type"></param>
		/// <param name="utilizerId"></param>
		/// <param name="membershipId"></param>
		/// <param name="document"></param>
		/// <param name="prior"></param>
		public ErtisAuthCustomEvent(string type, string utilizerId, string membershipId, dynamic document = null, dynamic prior = null)
		{
			this.EventType = type;
			this.UtilizerId = utilizerId;
			this.MembershipId = membershipId;
			this.Document = document;
			this.Prior = prior;
		}

		#endregion
	}
}