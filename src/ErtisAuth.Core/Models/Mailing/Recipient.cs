using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace ErtisAuth.Core.Models.Mailing;

public class Recipient
{
	#region Properties
	
	[JsonPropertyName("displayName")]
	[BsonElement("displayName")]
	[BsonIgnoreIfNull]
	public required string DisplayName { get; set; }
	
	[JsonPropertyName("emailAddress")]
	[BsonElement("emailAddress")]
	[BsonIgnoreIfNull]
	public required string EmailAddress { get; set; }
	
	#endregion
}