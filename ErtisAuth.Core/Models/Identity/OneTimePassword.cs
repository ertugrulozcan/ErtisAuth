using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class OneTimePassword : MembershipBoundedResource
{
    #region Properties
    
    [JsonProperty("user_id")]
    [JsonPropertyName("user_id")]
    [BsonElement("user_id")]
    public string? UserId { get; set; }
    
    [JsonProperty("email_address")]
    [JsonPropertyName("email_address")]
    [BsonElement("email_address")]
    public string? EmailAddress { get; set; }
    
    [JsonProperty("username")]
    [JsonPropertyName("username")]
    [BsonElement("username")]
    public string? Username { get; set; }
    
	/// <summary>
	/// The plain code, only in the response of the generation (to be delivered to the user); never stored.
	/// </summary>
	[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
	[JsonPropertyName("password")]
	[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonIgnore]
	public string? Password { get; set; }
	
	/// <summary>
	/// HMAC of the code; see OneTimePasswordService.
	/// </summary>
	[Newtonsoft.Json.JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	[BsonElement("password_hash")]
	public string? PasswordHash { get; set; }
	
	[Newtonsoft.Json.JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	[BsonElement("failed_attempts")]
	public int FailedAttempts { get; set; }
	
    [JsonProperty("token")]
    [JsonPropertyName("token")]
    [BsonElement("token")]
    public ResetPasswordToken? Token { get; set; }
    
    #endregion
}