using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.Events;

public class ErtisAuthEvent : ErtisAuthEventBase
{
	#region Properties
	
	[JsonProperty("event_type")]
	[JsonPropertyName("event_type")]
	[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
	[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
	public required ErtisAuthEventType EventType { get; set; }
	
	[JsonProperty("is_custom_event")] 
	[JsonPropertyName("is_custom_event")] 
	public override bool IsCustomEvent => false;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Default Constructor
	/// </summary>
	public ErtisAuthEvent()
	{
		
	}
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="user"></param>
	/// <param name="document"></param>
	/// <param name="prior"></param>
	public ErtisAuthEvent(User user, dynamic? document = null, dynamic? prior = null)
	{
		this.UtilizerId = user.Id;
		this.MembershipId = user.MembershipId;
		this.Document = document;
		this.Prior = prior;
	}
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="application"></param>
	/// <param name="document"></param>
	/// <param name="prior"></param>
	public ErtisAuthEvent(Application application, dynamic? document = null, dynamic? prior = null)
	{
		this.UtilizerId = application.Id;
		this.MembershipId = application.MembershipId;
		this.Document = document;
		this.Prior = prior;
	}
	
	#endregion
}