using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Dto.Models.Identity;

public class ClientInfoDto
{
	#region Properties
	
	[BsonElement("ip_address")]
	public string? IPAddress { get; set; }
	
	[BsonElement("user_agent")]
	public string? UserAgent { get; set; }
	
	#endregion
}