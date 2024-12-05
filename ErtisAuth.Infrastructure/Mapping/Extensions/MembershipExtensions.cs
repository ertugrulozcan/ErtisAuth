using System.Linq;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Dto.Models.Identity;
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
            ResetPasswordTokenExpiresIn = dto.ResetPasswordTokenExpiresIn,
            MailProviders = dto.MailProviders?.Select(ToMailProvider).ToArray(),
            UserActivation = dto.UserActivation is "active" or "Active" ? Status.Active : Status.Passive,
            CodePolicy = dto.CodePolicy,
            OtpSettings = dto.OtpSettings?.ToModel(),
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
            ResetPasswordTokenExpiresIn = model.ResetPasswordTokenExpiresIn,
            MailProviders = model.MailProviders?.Select(x => MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(Newtonsoft.Json.JsonConvert.SerializeObject(x))).ToArray(),
            UserActivation = model.UserActivation.ToString().ToLower(),
            CodePolicy = model.CodePolicy,
            OtpSettings = model.OtpSettings?.ToDto(),
            Sys = model.Sys?.ToDto()
        };
    }
    
    private static IMailProvider ToMailProvider(BsonDocument bsonDocument)
    {
        return MailProviderJsonConverter.Deserialize(bsonDocument.ToJson());
    }
    
    private static OtpSettings ToModel(this OtpSettingsDto dto)
    {
        return new OtpSettings
        {
            Host = dto.Host,
            Policy = dto.Policy?.ToModel(),
        };
    }
		
    private static OtpSettingsDto ToDto(this OtpSettings model)
    {
        return new OtpSettingsDto
        {
            Host = model.Host,
            Policy = model.Policy?.ToDto()
        };
    }
    
    private static OtpPasswordPolicy ToModel(this OtpPasswordPolicyDto dto)
    {
        return new OtpPasswordPolicy
        {
            Length = dto.Length,
            ContainsLetters = dto.ContainsLetters,
            ContainsDigits = dto.ContainsDigits,
            ExpiresIn = dto.ExpiresIn
        };
    }
		
    private static OtpPasswordPolicyDto ToDto(this OtpPasswordPolicy model)
    {
        return new OtpPasswordPolicyDto
        {
            Length = model.Length,
            ContainsLetters = model.ContainsLetters,
            ContainsDigits = model.ContainsDigits,
            ExpiresIn = model.ExpiresIn
        };
    }

    #endregion
}