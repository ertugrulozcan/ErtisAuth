using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Users;

public class UserWithPassword : User
{
	#region Properties
	
	[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
	[JsonPropertyName("password")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("password")]
	[BsonIgnoreIfNull]
	public string? Password { get; set; }
	
	#endregion
}