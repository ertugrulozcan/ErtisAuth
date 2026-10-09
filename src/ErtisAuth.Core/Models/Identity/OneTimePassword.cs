using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class OneTimePassword : MembershipBoundedResource
{
    #region Properties
	
    [JsonPropertyName("user_id")]
    [BsonElement("user_id")]
    public string? UserId { get; set; }
	
    [JsonPropertyName("email_address")]
    [BsonElement("email_address")]
    public string? EmailAddress { get; set; }
	
    [JsonPropertyName("username")]
    [BsonElement("username")]
    public string? Username { get; set; }
    
	/// <summary>
	/// The plain code, only in the response of the generation (to be delivered to the user); never stored.
	/// </summary>
	[JsonPropertyName("password")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonIgnore]
	public string? Password { get; set; }
	
	/// <summary>
	/// HMAC of the code; see OneTimePasswordService.
	/// </summary>
	[JsonIgnore]
	[BsonElement("password_hash")]
	public string? PasswordHash { get; set; }
	
	[JsonIgnore]
	[BsonElement("failed_attempts")]
	public int FailedAttempts { get; set; }
	
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; set; }
	
	/// <summary>
	/// Stored (not computed), so that the TTL index can delete the one-time password after it.
	/// </summary>
	[JsonPropertyName("expire_time")]
	[BsonElement("expire_time")]
	public DateTime ExpireTime { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public bool IsExpired => this.ExpireTime <= DateTime.UtcNow;
    
    #endregion
}