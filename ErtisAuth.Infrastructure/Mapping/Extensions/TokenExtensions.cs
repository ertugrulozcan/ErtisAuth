using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Infrastructure.Extensions;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class TokenExtensions
{
    #region Methods

    public static ActiveToken ToModel(this ActiveTokenDto dto)
    {
        return new ActiveToken
        {
            Id = dto.Id,
            MembershipId = dto.MembershipId,
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpiresIn = dto.ExpiresIn,
            RefreshTokenExpiresIn = dto.RefreshTokenExpiresIn,
            TokenType = dto.TokenType,
            CreatedAt = dto.CreatedAt,
            UserId = dto.UserId,
            UserName = dto.UserName,
            EmailAddress = dto.EmailAddress,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            ClientInfo = dto.ClientInfo?.ToModel()
        };
    }
		
    public static ActiveTokenDto ToDto(this ActiveToken model)
    {
        return new ActiveTokenDto
        {
            Id = model.Id,
            AccessToken = model.AccessToken,
            RefreshToken = model.RefreshToken,
            ExpiresIn = model.ExpiresIn,
            RefreshTokenExpiresIn = model.RefreshTokenExpiresIn,
            TokenType = model.TokenType,
            CreatedAt = model.CreatedAt,
            UserId = model.UserId,
            UserName = model.UserName,
            EmailAddress = model.EmailAddress,
            FirstName = model.FirstName,
            LastName = model.LastName,
            MembershipId = model.MembershipId,
            ClientInfo = model.ClientInfo?.ToDto()
        };
    }
    
    public static RevokedToken ToModel(this RevokedTokenDto dto)
    {
        return new RevokedToken
        {
            Id = dto.Id,
            MembershipId = dto.MembershipId,
            Token = dto.Token.AccessToken,
            UserId = dto.UserId,
            UserName = dto.UserName,
            EmailAddress = dto.EmailAddress,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            TokenType = dto.TokenType,
            RevokedAt = dto.RevokedAt
        };
    }

    #endregion
}