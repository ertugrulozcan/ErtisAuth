using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models;

public abstract class ResourceBase : IHasIdentifier
{
	#region Properties
	
	[JsonProperty("_id")]
	[JsonPropertyName("_id")]
	[BsonId]
	[BsonIgnoreIfDefault]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	
	#endregion
}