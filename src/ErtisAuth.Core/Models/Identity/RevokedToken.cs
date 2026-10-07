using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Core.Models.Identity;

public class RevokedToken : MembershipBoundedResource
{
	#region Properties
	
	[JsonPropertyName("token")]
	[BsonElement("token")]
	public string? Token { get; set; }
	
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
	
	[JsonPropertyName("token_type")]
	[BsonElement("token_type")]
	public string? TokenType { get; set; }
	
	/// <summary>
	/// The expiry of the revoked token itself; the revocation is needed until then.
	/// The TTL index of the collection deletes the record after this time.
	/// </summary>
	[JsonPropertyName("retain_until")]
	[BsonElement("retain_until")]
	public DateTime RetainUntil { get; set; }
	
	[JsonPropertyName("revoked_at")]
	[BsonElement("revoked_at")]
	public DateTime RevokedAt { get; set; }
	
	#endregion
}