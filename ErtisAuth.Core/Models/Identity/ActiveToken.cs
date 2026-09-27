using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using JsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Identity;

public class ActiveToken : MembershipBoundedResource
{
	#region Properties
	
	[JsonProperty("access_token")]
	[JsonPropertyName("access_token")]
	[BsonElement("access_token")]
	public required string AccessToken { get; set; }
	
	[JsonProperty("refresh_token")]
	[JsonPropertyName("refresh_token")]
	[BsonElement("refresh_token")]
	public string? RefreshToken { get; set; }
	
	[JsonProperty("expires_in")]
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresIn { get; set; }
	
	[JsonProperty("refresh_token_expires_in")]
	[JsonPropertyName("refresh_token_expires_in")]
	[BsonElement("refresh_token_expires_in")]
	public int RefreshTokenExpiresIn { get; set; }
	
	[JsonProperty("token_type")]
	[JsonPropertyName("token_type")]
	[BsonElement("token_type")]
	public string? TokenType { get; set; }
	
	[JsonProperty("created_at")]
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; set; }
	
	[JsonProperty("user_id")]
	[JsonPropertyName("user_id")]
	[BsonElement("user_id")]
	public string? UserId { get; set; }
	
	[JsonProperty("username")]
	[JsonPropertyName("username")]
	[BsonElement("username")]
	public string? UserName { get; set; }
	
	[JsonProperty("email_address")]
	[JsonPropertyName("email_address")]
	[BsonElement("email_address")]
	public string? EmailAddress { get; set; }
	
	[JsonProperty("first_name")]
	[JsonPropertyName("first_name")]
	[BsonElement("first_name")]
	public string? FirstName { get; set; }
	
	[JsonProperty("last_name")]
	[JsonPropertyName("last_name")]
	[BsonElement("last_name")]
	public string? LastName { get; set; }
	
	[JsonProperty("expire_time")]
	[JsonPropertyName("expire_time")]
	[BsonElement("expire_time")]
	public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
	
	[JsonIgnore]
	[NewtonsoftJsonIgnore]
	[BsonIgnore]
	public DateTime RefreshTokenExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.RefreshTokenExpiresIn));
	
	/// <summary>
	/// The record is needed until both tokens of the pair have expired: revoking either of them looks the record up
	/// (expire_time only covers the access token). The TTL index of the collection deletes the record after this time.
	/// </summary>
	[JsonProperty("retain_until")]
	[JsonPropertyName("retain_until")]
	[BsonElement("retain_until")]
	public DateTime RetainUntil => this.ExpireTime > this.RefreshTokenExpireTime ? this.ExpireTime : this.RefreshTokenExpireTime;
	
	[JsonProperty("client_info")]
	[JsonPropertyName("client_info")]
	[BsonElement("client_info")]
	public ClientInfo? ClientInfo { get; set; }
	
	#endregion
}