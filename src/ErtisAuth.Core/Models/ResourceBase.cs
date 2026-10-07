using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models;

public abstract class ResourceBase : IHasIdentifier
{
	#region Properties
	
	[JsonPropertyName("_id")]
	[BsonId]
	[BsonIgnoreIfDefault]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = null!;
	
	#endregion
}