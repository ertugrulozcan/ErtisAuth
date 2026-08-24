using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Extensions.Mailkit.Models;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class MailhookExtensions
{
    #region Methods
    
    public static MailHook ToModel(this MailHookDto dto)
    {
        return new MailHook
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            Slug = dto.Slug ?? string.Empty,
            Description = dto.Description,
            Event = dto.Event,
            Status = dto.Status,
            MailSubject = dto.MailSubject,
            MailTemplate = dto.MailTemplate,
            SendToUtilizer = dto.SendToUtilizer,
            Recipients = dto.Recipients?.Select(ToModel).ToArray(),
            FromName = dto.FromName,
            FromAddress = dto.FromAddress,
            MailProvider = dto.MailProvider,
            Variables = dto.Variables?.Select(ToModel).ToArray(),
            MembershipId = dto.MembershipId,
            Sys = dto.Sys?.ToModel()
        };
    }
    
    public static MailHookDto ToDto(this MailHook model)
    {
        return new MailHookDto
        {
            Id = model.Id,
            Name = model.Name,
            Slug = model.Slug,
            Description = model.Description,
            Event = model.Event,
            Status = model.Status,
            MailSubject = model.MailSubject,
            MailTemplate = model.MailTemplate,
            SendToUtilizer = model.SendToUtilizer,
            Recipients = model.Recipients?.Select(ToDto).ToArray(),
            FromName = model.FromName,
            FromAddress = model.FromAddress,
            MailProvider = model.MailProvider,
            Variables = model.Variables?.Select(ToDto).ToArray(), 
            MembershipId = model.MembershipId,
            Sys = model.Sys?.ToDto()
        };
    }
    
    private static MailHookVariable ToModel(this MailHookVariableDto dto)
    {
        return new MailHookVariable
        {
            Key = dto.Key,
            Value = dto.Value
        };
    }
    
    private static MailHookVariableDto ToDto(this MailHookVariable model)
    {
        return new MailHookVariableDto
        {
            Key = model.Key,
            Value = model.Value
        };
    }
    
    private static Recipient ToModel(this RecipientDto dto)
    {
        return new Recipient
        {
            DisplayName = dto.DisplayName ?? string.Empty,
            EmailAddress = dto.EmailAddress ?? string.Empty
        };
    }
    
    private static RecipientDto ToDto(this Recipient model)
    {
        return new RecipientDto
        {
            DisplayName = model.DisplayName,
            EmailAddress = model.EmailAddress
        };
    }
    
    #endregion
}