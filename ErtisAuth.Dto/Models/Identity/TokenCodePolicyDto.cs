using ErtisAuth.Dto.Models.Resources;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity;

public class TokenCodePolicyDto : EntityBase, IHasMembership, IHasSysDto
{
    #region Properties
    
    [BsonElement("name")]
    public string Name { get; set; }
		
    [BsonElement("slug")]
    public string Slug { get; set; }
    
    [BsonElement("description")]
    public string Description { get; set; }
    
    [BsonElement("length")]
    public int Length { get; set; }
    
    [BsonElement("contains_letters")]
    public bool ContainsLetters { get; set; }
    
    [BsonElement("contains_digits")]
    public bool ContainsDigits { get; set; }
    
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
    
    [BsonElement("membership_id")]
    public string MembershipId { get; set; }
    
    [BsonElement("sys")]
    public SysModelDto Sys { get; set; }
    
    #endregion
}