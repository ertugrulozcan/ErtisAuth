using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class OtpPasswordPolicy
{
	#region Constants
	
	public const int DefaultMaxAttempts = 5;
	
	#endregion
	
    #region Properties
	
    [JsonPropertyName("length")]
    [BsonElement("length")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [BsonIgnoreIfDefault]
    public int Length { get; set; }
	
    [JsonPropertyName("contains_letters")]
    [BsonElement("contains_letters")]
    public bool ContainsLetters { get; set; }
	
    [JsonPropertyName("contains_digits")]
    [BsonElement("contains_digits")]
    public bool ContainsDigits { get; set; }
	
    [JsonPropertyName("expires_in")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonElement("expires_in")]
    [BsonIgnoreIfNull]
    public int? ExpiresIn { get; set; }
	
	/// <summary>
	/// Failed verifications allowed per one-time password; reaching it deletes the one-time password.
	/// Memberships stored before this setting existed read the default.
	/// </summary>
	[JsonPropertyName("max_attempts")]
	[BsonElement("max_attempts")]
	public int MaxAttempts { get; set; } = DefaultMaxAttempts;
	
    #endregion
}