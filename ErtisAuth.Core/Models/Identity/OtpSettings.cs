using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity;

public class OtpSettings
{
    #region Properties
    
    [JsonProperty("host")]
    [JsonPropertyName("host")]
    [BsonElement("host")]
    [BsonIgnoreIfNull]
    public string? Host { get; set; }
    
    [JsonProperty("policy")]
    [JsonPropertyName("policy")]
    [BsonElement("policy")]
    [BsonIgnoreIfNull]
    public OtpPasswordPolicy? Policy { get; set; }
    
    #endregion
}