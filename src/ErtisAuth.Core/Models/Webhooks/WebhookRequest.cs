using System.Text.Json.Serialization;
using Ertis.Schema.Dynamics;
using ErtisAuth.Core.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Webhooks;

public class WebhookRequest
{
	#region Fields
	
	private DynamicObject? body;
	private BsonDocument? bodyDocument;
	
	#endregion
	
	#region Properties
	
	[JsonPropertyName("method")]
	[BsonElement("method")]
	public required string Method { get; set; }
	
	[JsonPropertyName("url")]
	[BsonElement("url")]
	public required string Url { get; set; }
	
	[JsonPropertyName("headers")]
	[BsonElement("headers")]
	public Dictionary<string, string>? Headers { get; set; }
	
	[JsonPropertyName("body")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonIgnore]
	public DynamicObject? Body
	{
		get => this.body;
		set
		{
			this.body = value;
			
			var json = value?.ToJson() ?? "{}";
			this.bodyDocument = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
		}
	}
	
	[JsonIgnore]
	[BsonElement("body")]
	[BsonIgnoreIfNull]
	public BsonDocument? BodyDocument
	{
		get => this.bodyDocument;
		set
		{
			this.bodyDocument = value;
			
			var json = value != null && !string.IsNullOrEmpty(value.ToString()) ? value.FixRegexOperators().ToJson() : "{}";
			this.body = DynamicObject.Parse(json);
		}
	}
	
	[JsonPropertyName("uncoveredBody")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[BsonElement("uncoveredBody")]
	[BsonIgnoreIfDefault]
	public bool UncoveredBody { get; set; }
	
	#endregion
}