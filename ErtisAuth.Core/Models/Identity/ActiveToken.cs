using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Core.Models.Identity;

public class ActiveToken : MembershipBoundedResource
{
	#region Properties
	
	[JsonPropertyName("access_token")]
	[BsonElement("access_token")]
	public required string AccessToken { get; set; }
	
	[JsonPropertyName("refresh_token")]
	[BsonElement("refresh_token")]
	public string? RefreshToken { get; set; }
	
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonPropertyName("refresh_token_expires_in")]
	[BsonElement("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonPropertyName("token_type")]
	[BsonElement("token_type")]
	public string? TokenType { get; set; }
	
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; set; }
	
	[JsonPropertyName("user_id")]
	[BsonElement("user_id")]
	public string? UserId { get; set; }
	
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public string? UserName { get; set; }
	
	[JsonPropertyName("email_address")]
	[BsonElement("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonPropertyName("first_name")]
	[BsonElement("first_name")]
	public string? FirstName { get; set; }
	
	[JsonPropertyName("last_name")]
	[BsonElement("last_name")]
	public string? LastName { get; set; }
	
	[JsonPropertyName("expire_time")]
	[BsonElement("expire_time")]
	public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
	
	[JsonIgnore]
	[BsonIgnore]
	public DateTime RefreshTokenExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.RefreshTokenExpiresIn));
	
	/// <summary>
	/// The record is needed until both tokens of the pair have expired: revoking either of them looks the record up
	/// (expire_time only covers the access token). The TTL index of the collection deletes the record after this time.
	/// </summary>
	[JsonPropertyName("retain_until")]
	[BsonElement("retain_until")]
	public DateTime RetainUntil => this.ExpireTime > this.RefreshTokenExpireTime ? this.ExpireTime : this.RefreshTokenExpireTime;
	
	[JsonPropertyName("client_info")]
	[BsonElement("client_info")]
	public ClientInfo? ClientInfo { get; set; }
	
	#endregion
}