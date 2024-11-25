using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity;

public class ResetPasswordTokenDto
{
    #region Properties

    [BsonElement("reset_token")]
    public string Token { get; set; }
    
    [BsonElement("expires_in")]
    public int ExpiresInTimeStamp { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }
    
    #endregion
}