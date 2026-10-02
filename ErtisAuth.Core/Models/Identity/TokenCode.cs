using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class TokenCode : MembershipBoundedResource
{
    #region Properties
    
    [JsonPropertyName("code")]
    [BsonElement("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("expires_in")]
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("created_at")]
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("expire_time")]
    [BsonElement("expire_time")]
    public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
    
    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonElement("user_id")]
    [BsonIgnoreIfNull]
    public string? UserId { get; set; }
    
    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonElement("token")]
    [BsonIgnoreIfNull]
    public BearerToken? Token { get; set; }
    
    #endregion
    
    #region Methods
    
    public void AssignToken(BearerToken bearerToken, string userId)
    {
        this.Token = bearerToken;
        this.UserId = userId;
    }
    
    #endregion
}