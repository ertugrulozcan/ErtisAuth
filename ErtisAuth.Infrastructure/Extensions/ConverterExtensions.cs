using System.Linq;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Dto.Extensions;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Resources;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Dto.Models.Webhooks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace ErtisAuth.Infrastructure.Extensions
{
    public static class ConverterExtensions
    {
        #region Sys

        public static SysModel ToModel(this SysModelDto dto)
        {
            return new SysModel
            {
                CreatedAt = dto.CreatedAt,
                CreatedBy = dto.CreatedBy,
                ModifiedAt = dto.ModifiedAt,
                ModifiedBy = dto.ModifiedBy
            };
        }
        
        public static SysModelDto ToDto(this SysModel model)
        {
            return new SysModelDto
            {
                CreatedAt = model.CreatedAt,
                CreatedBy = model.CreatedBy,
                ModifiedAt = model.ModifiedAt,
                ModifiedBy = model.ModifiedBy
            };
        }

        #endregion

        #region Membership

        public static Membership ToModel(this MembershipDto dto)
        {
            return new Membership
            {
                Id = dto.Id,
                Name = dto.Name,
                HashAlgorithm = dto.HashAlgorithm,
                DefaultEncoding = dto.DefaultEncoding,
                DefaultLanguage = dto.DefaultLanguage,
                ExpiresIn = dto.ExpiresIn,
                SecretKey = dto.SecretKey,
                RefreshTokenExpiresIn = dto.RefreshTokenExpiresIn,
                UserType = dto.UserType?.ToModel(),
                MailSettings = dto.MailSettings?.ToModel(),
                Sys = dto.Sys?.ToModel()
            };
        }
        
        public static MembershipDto ToDto(this Membership model)
        {
            return new MembershipDto
            {
                Id = model.Id,
                Name = model.Name,
                HashAlgorithm = model.HashAlgorithm,
                DefaultEncoding = model.DefaultEncoding,
                DefaultLanguage = model.DefaultLanguage,
                ExpiresIn = model.ExpiresIn,
                SecretKey = model.SecretKey,
                RefreshTokenExpiresIn = model.RefreshTokenExpiresIn,
                UserType = model.UserType?.ToDto(),
                MailSettings = model.MailSettings?.ToDto(),
                Sys = model.Sys?.ToDto()
            };
        }

        #endregion
        
        #region User

        public static User ToModel(this UserDto dto)
        {
            return new User
            {
                Id = dto.Id,
                Username = dto.Username,
                EmailAddress = dto.EmailAddress,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                Forbidden = dto.Forbidden,
                Permissions = dto.Permissions,
                AdditionalProperties = dto.AdditionalProperties?.ToDynamicObject(),
                MembershipId = dto.MembershipId,
                Sys = dto.Sys?.ToModel()
            };
        }
        
        public static UserDto ToDto(this User model)
        {
            BsonDocument additionalProperties = null;
            if (model.AdditionalProperties != null)
            {
                string documentJson = JsonConvert.SerializeObject(model.AdditionalProperties);
                additionalProperties = BsonDocument.Parse(documentJson);
            }
            
            return new UserDto
            {
                Id = model.Id,
                Username = model.Username,
                EmailAddress = model.EmailAddress,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                Forbidden = model.Forbidden,
                Permissions = model.Permissions,
                AdditionalProperties = additionalProperties,
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
        
        #region UserType

        public static UserType ToModel(this UserTypeDto dto)
        {
            return new UserType
            {
                Title = dto.Title,
                Description = dto.Description,
                Properties = dto.Properties?.ToDynamicObject(),
                RequiredFields = dto.RequiredFields
            };
        }
        
        public static UserTypeDto ToDto(this UserType model)
        {
            var schemaJson = model.Properties.ToString();
            var schema = BsonDocument.Parse(schemaJson);
            
            return new UserTypeDto
            {
                Name = model.Name,
                Title = model.Title,
                Description = model.Description,
                Properties = schema,
                RequiredFields = model.RequiredFields
            };
        }

        #endregion

        #region Webhook

        public static Webhook ToModel(this WebhookDto dto)
        {
            return new Webhook
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Event = dto.Event,
                Status = dto.Status,
                RequestList = dto.RequestList?.Select(x => x.ToModel()).ToArray(),
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
                Status = model.Status,
                RequestList = model.RequestList?.Select(x => x.ToDto()).ToArray(),
                TryCount = model.TryCount,
                MembershipId = model.MembershipId,
                Sys = model.Sys?.ToDto()
            };
        }
        
        public static WebhookRequest ToModel(this WebhookRequestDto dto)
        {
            return new WebhookRequest
            {
                Url = dto.Url,
                Method = dto.Method,
                Headers = dto.Headers,
                Body = dto.Body?.ToDynamicObject()
            };
        }
        
        public static WebhookRequestDto ToDto(this WebhookRequest model)
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
        
        #region Mailing

        public static MailHook ToModel(this MailHookDto dto)
        {
            return new MailHook
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Event = dto.Event,
                Status = dto.Status,
                MailSubject = dto.MailSubject,
                MailTemplate = dto.MailTemplate,
                FromName = dto.FromName,
                FromAddress = dto.FromAddress,
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
                Description = model.Description,
                Event = model.Event,
                Status = model.Status,
                MailSubject = model.MailSubject,
                MailTemplate = model.MailTemplate,
                FromName = model.FromName,
                FromAddress = model.FromAddress,
                MembershipId = model.MembershipId,
                Sys = model.Sys?.ToDto()
            };
        }
        
        public static MembershipMailSettings ToModel(this MembershipMailSettingsDto dto)
        {
            return new MembershipMailSettings
            {
                SmtpServer = dto.SmtpServer?.ToModel()
            };
        }
        
        public static MembershipMailSettingsDto ToDto(this MembershipMailSettings model)
        {
            return new MembershipMailSettingsDto
            {
                SmtpServer = model.SmtpServer?.ToDto()
            };
        }
        
        public static SmtpServer ToModel(this SmtpServerDto dto)
        {
            return new SmtpServer
            {
                Host = dto.Host,
                Port = dto.Port,
                TlsEnabled = dto.TlsEnabled,
                Username = dto.Username,
                Password = dto.Password
            };
        }
        
        public static SmtpServerDto ToDto(this SmtpServer model)
        {
            return new SmtpServerDto
            {
                Host = model.Host,
                Port = model.Port,
                TlsEnabled = model.TlsEnabled,
                Username = model.Username,
                Password = model.Password
            };
        }

        #endregion
    }
}