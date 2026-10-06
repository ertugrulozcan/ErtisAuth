using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

/// <summary>
/// A device login (e.g. a smart TV), like the OAuth 2.0 device authorization grant (RFC 8628): the short user code is
/// shown on the device and approved by a signed in user; only the device knows the device code, which is exchanged for
/// a token. The device code is stored as a hash and no token is stored, it is generated when the device gets it.
/// </summary>
public class TokenCode : MembershipBoundedResource
{
	#region Properties
	
	[JsonPropertyName("user_code")]
	[BsonElement("user_code")]
	public required string UserCode { get; set; }
	
	[JsonIgnore]
	[BsonElement("device_code_hash")]
	public string? DeviceCodeHash { get; set; }
	
	/// <summary>
	/// One of <see cref="TokenCodeStatus"/>.
	/// </summary>
	[JsonPropertyName("status")]
	[BsonElement("status")]
	public string Status { get; set; } = TokenCodeStatus.Pending;
	
	[JsonPropertyName("expires_in")]
	[BsonElement("expires_in")]
	public int ExpiresIn { get; set; }
	
	/// <summary>
	/// The minimum number of seconds between two polls of the device.
	/// </summary>
	[JsonPropertyName("interval")]
	[BsonElement("interval")]
	public int Interval { get; set; }
	
	[JsonPropertyName("created_at")]
	[BsonElement("created_at")]
	public DateTime CreatedAt { get; set; }
	
	/// <summary>
	/// Stored (not computed), so that the atomic updates can filter on it; the TTL index deletes the record after it.
	/// </summary>
	[JsonPropertyName("expire_time")]
	[BsonElement("expire_time")]
	public DateTime ExpireTime { get; set; }
	
	/// <summary>
	/// The device that requested the code, shown to the user before the approval and stored with the session.
	/// </summary>
	[JsonPropertyName("client_info")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("client_info")]
	[BsonIgnoreIfNull]
	public ClientInfo? ClientInfo { get; set; }
	
	/// <summary>
	/// The user who approved or denied the code.
	/// </summary>
	[JsonPropertyName("user_id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("user_id")]
	[BsonIgnoreIfNull]
	public string? UserId { get; set; }
	
	[JsonPropertyName("decided_at")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[BsonElement("decided_at")]
	[BsonIgnoreIfNull]
	public DateTime? DecidedAt { get; set; }
	
	[JsonIgnore]
	[BsonElement("last_polled_at")]
	[BsonIgnoreIfNull]
	public DateTime? LastPolledAt { get; set; }
	
	[JsonIgnore]
	[BsonIgnore]
	public bool IsExpired => this.ExpireTime <= DateTime.UtcNow;
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// The form a user code is stored and compared in: upper case, without the separators a device may show
	/// (e.g. "k7q2-x" is "K7Q2X").
	/// </summary>
	public static string NormalizeUserCode(string? userCode)
	{
		return string.IsNullOrEmpty(userCode)
			? string.Empty
			: new string(userCode.Where(x => x != '-' && !char.IsWhiteSpace(x)).ToArray()).ToUpperInvariant();
	}
	
	#endregion
}
