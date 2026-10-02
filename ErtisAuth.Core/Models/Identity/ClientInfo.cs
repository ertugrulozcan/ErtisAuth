using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class ClientInfo
{
	#region Properties
	
	[JsonPropertyName("ip_address")]
	[BsonElement("ip_address")]
	[BsonIgnoreIfNull]
	public string? IPAddress { get; set; }
	
	[JsonPropertyName("user_agent")]
	[BsonElement("user_agent")]
	[BsonIgnoreIfNull]
	public string? UserAgent { get; set; }
	
	#endregion
}