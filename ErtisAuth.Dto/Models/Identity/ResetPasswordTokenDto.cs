using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Dto.Models.Identity;

public class ResetPasswordTokenDto
{
    #region Properties
    
    [BsonElement("reset_token")]
    public required string Token { get; set; }
    
    [BsonElement("expires_in")]
    public int ExpiresInTimeStamp { get; set; }
    
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }
    
    #endregion
}