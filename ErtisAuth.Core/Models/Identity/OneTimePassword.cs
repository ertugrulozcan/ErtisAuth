using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class OneTimePassword : MembershipBoundedResource
{
    #region Properties
    
    [JsonProperty("user_id")]
    [JsonPropertyName("user_id")]
    [BsonElement("user_id")]
    public string? UserId { get; set; }
    
    [JsonProperty("email_address")]
    [JsonPropertyName("email_address")]
    [BsonElement("email_address")]
    public string? EmailAddress { get; set; }
    
    [JsonProperty("username")]
    [JsonPropertyName("username")]
    [BsonElement("username")]
    public string? Username { get; set; }
    
    [JsonProperty("password")]
    [JsonPropertyName("password")]
    [BsonElement("password")]
    public string? Password { get; set; }
    
    [JsonProperty("token")]
    [JsonPropertyName("token")]
    [BsonElement("token")]
    public ResetPasswordToken? Token { get; set; }
    
    #endregion
}