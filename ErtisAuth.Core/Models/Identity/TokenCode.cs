using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class TokenCode : MembershipBoundedResource
{
    #region Properties

    [JsonProperty("code")]
    [JsonPropertyName("code")]
    public string Code { get; set; }
    
    [JsonProperty("expires_in")]
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonProperty("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonProperty("expire_time")]
    [JsonPropertyName("expire_time")]
    public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
    
    [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("user_id")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string UserId { get; set; }
    
    [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("token")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BearerToken Token { get; set; }
    
    #endregion

    #region Methods

    public void AssignToken(BearerToken bearerToken, string userId)
    {
        this.Token = bearerToken;
        this.UserId = userId;
    }

    #endregion
}