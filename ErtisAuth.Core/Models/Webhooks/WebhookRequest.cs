using System.Text.Json.Serialization;
using Ertis.Schema.Dynamics;
using Ertis.Schema.Serialization;
using ErtisAuth.Core.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using NewtonsoftJsonProperty = Newtonsoft.Json.JsonPropertyAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Models.Webhooks;

public class WebhookRequest
{
	#region Fields
	
	private DynamicObject? body;
	private BsonDocument? bodyDocument;
	
	#endregion
	
	#region Properties
	
	[JsonPropertyName("method")]
	[NewtonsoftJsonProperty("method")]
	[BsonElement("method")]
	public required string Method { get; set; }
	
	[JsonPropertyName("url")]
	[NewtonsoftJsonProperty("url")]
	[BsonElement("url")]
	public required string Url { get; set; }
	
	[JsonPropertyName("headers")]
	[NewtonsoftJsonProperty("headers")]
	[BsonElement("headers")]
	public Dictionary<string, string>? Headers { get; set; }
	
	[JsonPropertyName("body")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter(typeof(DynamicObjectJsonConverter))]
	[NewtonsoftJsonProperty("body", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
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
	[NewtonsoftJsonIgnore]
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
	[NewtonsoftJsonProperty("uncoveredBody", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
	[BsonElement("uncoveredBody")]
	[BsonIgnoreIfDefault]
	public bool UncoveredBody { get; set; }
	
	#endregion
}