using System;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class TokenExtensions
{
    #region Bearer Token Methods

    private static BearerToken ToModel(this BearerTokenDto dto)
    {
        return new BearerToken(
            dto.AccessToken, 
            TimeSpan.FromSeconds(dto.ExpiresIn), 
            dto.RefreshToken, 
            TimeSpan.FromSeconds(dto.RefreshTokenExpiresIn));
    }

    private static BearerTokenDto ToDto(this BearerToken model)
    {
        return new BearerTokenDto
        {
            AccessToken = model.AccessToken,
            RefreshToken = model.RefreshToken,
            ExpiresIn = model.ExpiresInTimeStamp,
            RefreshTokenExpiresIn = model.RefreshTokenExpiresInTimeStamp 
        };
    }

    #endregion
    
    #region Active Token Methods

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
    
    #region Token Code Methods

    public static TokenCode ToModel(this TokenCodeDto dto)
    {
        return new TokenCode
        {
            Id = dto.Id,
            MembershipId = dto.MembershipId,
            Code = dto.Code,
            ExpiresIn = dto.ExpiresIn,
            CreatedAt = dto.CreatedAt,
            UserId = dto.UserId,
            Token = dto.Token?.ToModel()
        };
    }
		
    public static TokenCodeDto ToDto(this TokenCode model)
    {
        return new TokenCodeDto
        {
            Id = model.Id,
            Code = model.Code,
            ExpiresIn = model.ExpiresIn,
            CreatedAt = model.CreatedAt,
            UserId = model.UserId,
            Token = model.Token?.ToDto(),
            MembershipId = model.MembershipId
        };
    }
    
    public static TokenCodePolicy ToModel(this TokenCodePolicyDto dto)
    {
        return new TokenCodePolicy
        {
            Id = dto.Id,
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            Length = dto.Length,
            ContainsLetters = dto.ContainsLetters,
            ContainsDigits = dto.ContainsDigits,
            ExpiresIn = dto.ExpiresIn,
            MembershipId = dto.MembershipId,
            Sys = dto.Sys?.ToModel()
        };
    }
		
    public static TokenCodePolicyDto ToDto(this TokenCodePolicy model)
    {
        return new TokenCodePolicyDto
        {
            Id = model.Id,
            Name = model.Name,
            Slug = model.Slug,
            Description = model.Description,
            Length = model.Length,
            ContainsLetters = model.ContainsLetters,
            ContainsDigits = model.ContainsDigits,
            ExpiresIn = model.ExpiresIn,
            MembershipId = model.MembershipId,
            Sys = model.Sys?.ToDto()
        };
    }
    
    #endregion

    #region Reset Password Token Methods

    public static ResetPasswordToken ToModel(this ResetPasswordTokenDto dto)
    {
        return new ResetPasswordToken(dto.Token, TimeSpan.FromSeconds(dto.ExpiresInTimeStamp), dto.CreatedAt);
    }
		
    public static ResetPasswordTokenDto ToDto(this ResetPasswordToken model)
    {
        return new ResetPasswordTokenDto
        {
            Token = model.Token,
            ExpiresInTimeStamp = model.ExpiresInTimeStamp,
            CreatedAt = model.CreatedAt
        };
    }

    #endregion
    
    #region Onetime Token Methods

    public static OneTimePassword ToModel(this OneTimePasswordDto dto)
    {
        return new OneTimePassword
        {
            Id = dto.Id,
            UserId = dto.UserId,
            EmailAddress = dto.EmailAddress,
            Username = dto.Username,
            Password = dto.Password,
            Token = dto.Token.ToModel(),
            MembershipId = dto.MembershipId
        };
    }
		
    public static OneTimePasswordDto ToDto(this OneTimePassword model)
    {
        return new OneTimePasswordDto
        {
            Id = model.Id,
            UserId = model.UserId,
            EmailAddress = model.EmailAddress,
            Username = model.Username,
            Password = model.Password,
            Token = model.Token.ToDto(),
            MembershipId = model.MembershipId
        };
    }

    #endregion
}