using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using SystemJsonIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace ErtisAuth.Core.Models.Identity;

public class TokenCode : MembershipBoundedResource
{
    #region Properties
    
    [JsonProperty("code")]
    [JsonPropertyName("code")]
    [BsonElement("code")]
    public string? Code { get; set; }
    
    [JsonProperty("expires_in")]
    [JsonPropertyName("expires_in")]
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonProperty("created_at")]
    [JsonPropertyName("created_at")]
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonProperty("expire_time")]
    [JsonPropertyName("expire_time")]
    [BsonElement("expire_time")]
    public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
    
    [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("user_id")]
    [SystemJsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonElement("user_id")]
    [BsonIgnoreIfNull]
    public string? UserId { get; set; }
    
    [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("token")]
    [SystemJsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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