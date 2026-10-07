using System.Text.Json;
using System.Text.Json.Serialization;
using Ertis.MongoDB.Serialization;
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
	
	/// <summary>
	/// The payloads (e.g. anonymous objects of models) may contain ObjectIds (DynamicObjects carry their own converter)
	/// </summary>
	private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
	{
		Converters =
		{
			new ObjectIdConverter()
		}
	};
	
	private object? document;
	private object? prior;
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
		get => this.document;
		set
		{
			this.document = value;
			this.bsonDocument = ToBsonDocument(value);
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
			// Read from the database: the stored document is kept as it is
			this.bsonDocument = value;
			this.document = value?.ToDynamicObject();
		}
	}
	
	[JsonPropertyName("prior")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonIgnore]
	public object? Prior
	{
		get => this.prior;
		set
		{
			this.prior = value;
			this.bsonPrior = ToBsonDocument(value);
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
			// Read from the database: the stored document is kept as it is
			this.bsonPrior = value;
			this.prior = value?.ToDynamicObject();
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
	
	#region Methods
	
	private static BsonDocument? ToBsonDocument(object? payload)
	{
		return payload switch
		{
			null => null,
			BsonDocument bsonDocument => bsonDocument,
			DynamicObject dynamicObject => BsonDocument.Parse(dynamicObject.ToJson()),
			_ => BsonDocument.Parse(JsonSerializer.Serialize(payload, PayloadSerializerOptions))
		};
	}
	
	#endregion
}