using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Models.Identity;

public class OtpSettings
{
    #region Properties
    
    [JsonPropertyName("host")]
    [BsonElement("host")]
    [BsonIgnoreIfNull]
    public string? Host { get; set; }
    
    [JsonPropertyName("policy")]
    [BsonElement("policy")]
    [BsonIgnoreIfNull]
    public OtpPasswordPolicy? Policy { get; set; }
    
    #endregion
}