using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class OneTimePassword : MembershipBoundedResource
{
    #region Properties

    [JsonProperty("user_id")]
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    
    [JsonProperty("email_address")]
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; }
    
    [JsonProperty("username")]
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonProperty("password")]
    [JsonPropertyName("password")]
    public string Password { get; set; }
    
    [JsonProperty("token")]
    [JsonPropertyName("token")]
    public ResetPasswordToken Token { get; set; }
    
    #endregion
}