using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity;

public class TokenCodeDto : EntityBase, IHasMembership
{
    #region Properties
    
    [BsonElement("code")]
    public string Code { get; set; }
    
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
    
    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime CreatedAt { get; set; }
    	
    [BsonElement("expire_time")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
    
    [BsonElement("user_id")]
    public string UserId { get; set; }
    
    [BsonElement("token")]
    public BearerTokenDto Token { get; set; }
		
    [BsonElement("membership_id")]
    public string MembershipId { get; set; }
    
    #endregion
}