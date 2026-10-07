using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Core.Models;

public interface IHasSlug
{
	#region Properties
	
	[JsonPropertyName("slug")]
	[BsonElement("slug")]
	public string Slug { get; }
	
	#endregion
}