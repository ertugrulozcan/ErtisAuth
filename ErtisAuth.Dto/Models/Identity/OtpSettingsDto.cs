using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Dto.Models.Identity;

public class OtpSettingsDto
{
    #region Properties
    
    [BsonElement("host")]
    public string? Host { get; set; }
    
    [BsonElement("policy")]
    public OtpPasswordPolicyDto? Policy { get; set; }
    
    #endregion
}