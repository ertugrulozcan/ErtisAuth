using MongoDB.Bson.Serialization.Attributes;

namespace ErtisAuth.Dto.Models.Identity;

public class BearerTokenDto
{
    #region Properties

    [BsonElement("access_token")]
    public string AccessToken { get; set; }
    
    [BsonElement("refresh_token")]
    public string RefreshToken { get; set; }
    
    [BsonElement("expires_in")]
    public int ExpiresIn { get; set; }
		
    [BsonElement("refresh_token_expires_in")]
    public int RefreshTokenExpiresIn { get; set; }
    
    #endregion
}