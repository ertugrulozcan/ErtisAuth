using System.Linq;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Extensions.Mailkit.Providers;
using ErtisAuth.Extensions.Mailkit.Serialization;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class MembershipExtensions
{
    #region Methods

    public static Membership ToModel(this MembershipDto dto)
    {
        return new Membership
        {
            Id = dto.Id,
            Name = dto.Name,
            Slug = dto.Slug,
            HashAlgorithm = dto.HashAlgorithm,
            DefaultEncoding = dto.DefaultEncoding,
            DefaultLanguage = dto.DefaultLanguage,
            ExpiresIn = dto.ExpiresIn,
            SecretKey = dto.SecretKey,
            RefreshTokenExpiresIn = dto.RefreshTokenExpiresIn,
            MailProviders = dto.MailProviders?.Select(ToMailProvider).ToArray(),
            UserActivation = dto.UserActivation is "active" or "Active" ? Status.Active : Status.Passive,
            Sys = dto.Sys?.ToModel()
        };
    }
        
    public static MembershipDto ToDto(this Membership model)
    {
        return new MembershipDto
        {
            Id = model.Id,
            Name = model.Name,
            Slug = model.Slug,
            HashAlgorithm = model.HashAlgorithm,
            DefaultEncoding = model.DefaultEncoding,
            DefaultLanguage = model.DefaultLanguage,
            ExpiresIn = model.ExpiresIn,
            SecretKey = model.SecretKey,
            RefreshTokenExpiresIn = model.RefreshTokenExpiresIn,
            MailProviders = model.MailProviders?.Select(x => MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(Newtonsoft.Json.JsonConvert.SerializeObject(x))).ToArray(),
            UserActivation = model.UserActivation.ToString().ToLower(),
            Sys = model.Sys?.ToDto()
        };
    }
    
    private static IMailProvider ToMailProvider(BsonDocument bsonDocument)
    {
        return MailProviderJsonConverter.Deserialize(bsonDocument.ToJson());
    }

    #endregion
}