using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class TokenCode : MembershipBoundedResource
{
    #region Properties

    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonProperty("expire_time")]
    public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
    
    [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
    public string UserId { get; set; }
    
    [JsonProperty("token", NullValueHandling = NullValueHandling.Ignore)]
    public BearerToken Token { get; set; }
    
    #endregion
}