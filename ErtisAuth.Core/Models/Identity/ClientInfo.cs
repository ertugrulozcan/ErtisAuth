using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class ClientInfo
{
	#region Properties
	
	[JsonProperty("ip_address")]
	[JsonPropertyName("ip_address")]
	[BsonElement("ip_address")]
	[BsonIgnoreIfNull]
	public string? IPAddress { get; set; }
	
	[JsonProperty("user_agent")]
	[JsonPropertyName("user_agent")]
	[BsonElement("user_agent")]
	[BsonIgnoreIfNull]
	public string? UserAgent { get; set; }
	
	#endregion
}