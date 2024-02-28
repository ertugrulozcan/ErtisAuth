using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Dto.Extensions;
using ErtisAuth.Dto.Models.Webhooks;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class WebhookExtensions
{
    #region Methods

    public static Webhook ToModel(this WebhookDto dto)
    {
        return new Webhook
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Event = dto.Event,
            Status = dto.Status is "active" or "Active" ? WebhookStatus.Active : dto.Status is "passive" or "Passive" ? WebhookStatus.Passive : null,
            Request = dto.Request?.ToModel(),
            TryCount = dto.TryCount,
            MembershipId = dto.MembershipId,
            Sys = dto.Sys?.ToModel()
        };
    }
    
    public static WebhookDto ToDto(this Webhook model)
    {
        return new WebhookDto
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            Event = model.Event,
            Status = model.Status?.ToString().ToLower(),
            Request = model.Request?.ToDto(),
            TryCount = model.TryCount,
            MembershipId = model.MembershipId,
            Sys = model.Sys?.ToDto()
        };
    }
    
    private static WebhookRequest ToModel(this WebhookRequestDto dto)
    {
        return new WebhookRequest
        {
            Url = dto.Url,
            Method = dto.Method,
            Headers = dto.Headers,
            Body = dto.Body?.ToDynamicObject()
        };
    }
    
    private static WebhookRequestDto ToDto(this WebhookRequest model)
    {
        BsonDocument body = null;
        if (model.Body != null)
        {
            var json = model.Body.ToString();
            if (!string.IsNullOrEmpty(json))
            {
                body = BsonDocument.Parse(json);    
            }
        }
        
        return new WebhookRequestDto
        {
            Url = model.Url,
            Method = model.Method,
            Headers = model.Headers,
            Body = body
        };
    }

    #endregion
}