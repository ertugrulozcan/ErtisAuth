using System.Text.Json;
using System.Text.Json.Serialization;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Core.Models.Events;

public class ErtisAuthEvent : ResourceBase, IErtisAuthEvent, IHasMembership
{
	#region Fields
	
	private BsonDocument? bsonDocument;
	private BsonDocument? bsonPrior;
	
	#endregion
	
	#region Properties
	
	[JsonPropertyName("event_type")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	[BsonElement("event_type")]
	[BsonRepresentation(BsonType.String)]
	public required ErtisAuthEventType EventType { get; set; }
	
	[JsonPropertyName("utilizer_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("utilizer_id")]
	public required string UtilizerId { get; set; }
	
	[JsonPropertyName("document")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonIgnore]
	public object? Document
	{
		get;
		set
		{
			field = value;
			
			if (value != null)
			{
				if (value is DynamicObject dynamicObject)
				{
					this.bsonDocument = BsonDocument.Parse(dynamicObject.ToJson());
				}
				else if (value is BsonDocument _bsonDocument)
				{
					this.bsonDocument = _bsonDocument;
				}
				else
				{
					var documentJson = JsonSerializer.Serialize(value);
					this.bsonDocument = BsonDocument.Parse(documentJson);
				}
			}
		}
	}
	
	[JsonIgnore]
	[BsonElement("document")]
	[BsonIgnoreIfNull]
	public BsonDocument? BsonDocument
	{
		get => this.bsonDocument;
		set
		{
			this.bsonDocument = value;
			this.Document = value?.ToDynamicObject();
		}
	}
	
	[JsonPropertyName("prior")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonIgnore]
	public object? Prior
	{
		get;
		set
		{
			field = value;
			
			if (value != null)
			{
				if (value is DynamicObject dynamicObject)
				{
					this.bsonPrior = BsonDocument.Parse(dynamicObject.ToJson());
				}
				else if (value is BsonDocument _bsonPrior)
				{
					this.bsonPrior = _bsonPrior;
				}
				else
				{
					var documentJson = JsonSerializer.Serialize(value);
					this.bsonPrior = BsonDocument.Parse(documentJson);
				}
			}
		}
	}
	
	[JsonIgnore]
	[BsonElement("prior")]
	[BsonIgnoreIfNull]
	public BsonDocument? BsonPrior
	{
		get => this.bsonPrior;
		set
		{
			this.bsonPrior = value;
			this.Prior = value?.ToDynamicObject();
		}
	}
	
	[JsonPropertyName("event_time")]
	[BsonElement("event_time")]
	public DateTime EventTime { get; set; }
	
	[JsonPropertyName("membership_id")]
	[BsonElement("membership_id")]
	public required string MembershipId { get; set; }
	
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