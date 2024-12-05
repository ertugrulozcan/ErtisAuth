using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity;

public class OtpPasswordPolicyDto
{
    #region Properties

    [BsonElement("length")]
    public int Length { get; set; }
    
    [BsonElement("contains_letters")]
    public bool ContainsLetters { get; set; }
    
    [BsonElement("contains_digits")]
    public bool ContainsDigits { get; set; }
    
    [BsonElement("expires_in")]
    public int? ExpiresIn { get; set; }

    #endregion
}