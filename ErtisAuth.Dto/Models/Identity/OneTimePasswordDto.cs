using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity;

public class OneTimePasswordDto : EntityBase, IHasMembership
{
    #region Properties

    [BsonElement("user_id")]
    public string UserId { get; set; }
    
    [BsonElement("email_address")]
    public string EmailAddress { get; set; }
    
    [BsonElement("username")]
    public string Username { get; set; }
    
    [BsonElement("password")]
    public string Password { get; set; }
    
    [BsonElement("token")]
    public ResetPasswordTokenDto Token { get; set; }
    
    [BsonElement("membership_id")]
    public string MembershipId { get; set; }
    
    #endregion
}