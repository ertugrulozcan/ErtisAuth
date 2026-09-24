using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Mailing;

public class Recipient
{
	#region Properties
	
	[JsonProperty("displayName")]
	[JsonPropertyName("displayName")]
	[BsonElement("displayName")]
	[BsonIgnoreIfNull]
	public required string DisplayName { get; set; }
	
	[JsonProperty("emailAddress")]
	[JsonPropertyName("emailAddress")]
	[BsonElement("emailAddress")]
	[BsonIgnoreIfNull]
	public required string EmailAddress { get; set; }
	
	#endregion
}