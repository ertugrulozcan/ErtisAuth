using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class UserExtensions
{
    #region Methods

    public static User ToModel(this UserDto dto)
    {
        return new User
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            EmailAddress = dto.EmailAddress,
            Role = dto.Role,
            Permissions = dto.Permissions,
            Forbidden = dto.Forbidden,
            UserType = dto.UserType,
            SourceProvider = string.IsNullOrEmpty(dto.SourceProvider) ? KnownProviders.ErtisAuth.ToString() : dto.SourceProvider,
            ConnectedAccounts = dto.ConnectedAccounts,
            IsActive = dto.IsActive,
            MembershipId = dto.MembershipId,
            Sys = dto.Sys?.ToModel()
        };
    }
    
    public static UserDto ToDto(this User model)
    {
        return new UserDto
        {
            Id = model.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Username = model.Username,
            EmailAddress = model.EmailAddress,
            Role = model.Role,
            Permissions = model.Permissions,
            Forbidden = model.Forbidden,
            UserType = model.UserType,
            SourceProvider = model.SourceProvider,
            ConnectedAccounts = model.ConnectedAccounts,
            IsActive = model.IsActive,
            MembershipId = model.MembershipId,
            Sys = model.Sys?.ToDto()
        };
    }
    
    public static UserDto ToDto(this UserWithPasswordHash model)
    {
        var dto = (model as User).ToDto();
        dto.PasswordHash = model.PasswordHash;
        return dto;
    }

    #endregion
}