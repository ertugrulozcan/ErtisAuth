using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverterAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverterAttribute;
using NewtonsoftStringEnumConverter = Newtonsoft.Json.Converters.StringEnumConverter;

namespace ErtisAuth.Core.Models.Events;

public class ErtisAuthEvent : ResourceBase, IErtisAuthEvent, IHasMembership
{
	#region Properties
	
	[JsonProperty("event_type")]
	[JsonPropertyName("event_type")]
	[BsonElement("event_type")]
	[NewtonsoftJsonConverter(typeof(NewtonsoftStringEnumConverter))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonRepresentation(BsonType.String)]
	public required ErtisAuthEventType EventType { get; set; }
	
	[JsonProperty("utilizer_id")]
	[JsonPropertyName("utilizer_id")]
	[BsonElement("utilizer_id")]
	public required string UtilizerId { get; set; }
	
	[JsonProperty("membership_id")]
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
	[JsonProperty("document")]
	[JsonPropertyName("document")]
	[BsonElement("document")]
	public dynamic? Document { get; set; }
	
	[JsonProperty("prior")]
	[JsonPropertyName("prior")]
	[BsonElement("prior")]
	public dynamic? Prior { get; set; }
	
	[JsonProperty("event_time")]
	[JsonPropertyName("event_time")]
	[BsonElement("event_time")]
	public DateTime EventTime { get; set; }
	
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