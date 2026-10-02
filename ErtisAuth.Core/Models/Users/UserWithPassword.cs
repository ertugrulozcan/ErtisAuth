using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Users;

public class UserWithPassword : User
{
	#region Properties
	
	[JsonPropertyName("password")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("password")]
	[BsonIgnoreIfNull]
	public string? Password { get; set; }
	
	#endregion
}