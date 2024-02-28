using System;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Dto.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class ProviderExtensions
{
    #region Methods

    public static Provider ToModel(this ProviderDto dto)
    {
        return new Provider(TryParseEnum<KnownProviders>(dto.Name))
        {
            Id = dto.Id,
            Description = dto.Description,
            DefaultRole = dto.DefaultRole,
            DefaultUserType = dto.DefaultUserType,
            AppClientId = dto.AppClientId,
            TeamId = dto.TeamId,
            TenantId = dto.TenantId,
            PrivateKey = dto.PrivateKey,
            PrivateKeyId = dto.PrivateKeyId,
            RedirectUri = dto.RedirectUri,
            IsActive = dto.IsActive,
            MembershipId = dto.MembershipId,
            Sys = dto.Sys?.ToModel()
        };
    }
        
    public static ProviderDto ToDto(this Provider model)
    {
        return new ProviderDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            DefaultRole = model.DefaultRole,
            DefaultUserType = model.DefaultUserType,
            AppClientId = model.AppClientId,
            TeamId = model.TeamId,
            TenantId = model.TenantId,
            PrivateKey = model.PrivateKey,
            PrivateKeyId = model.PrivateKeyId,
            RedirectUri = model.RedirectUri,
            IsActive = model.IsActive,
            MembershipId = model.MembershipId,
            Sys = model.Sys?.ToDto()
        };
    }
    
    private static TEnum TryParseEnum<TEnum>(string str, TEnum defaultValue = default) where TEnum : struct
    {
        try
        {
            return Enum.Parse<TEnum>(str);
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion
}