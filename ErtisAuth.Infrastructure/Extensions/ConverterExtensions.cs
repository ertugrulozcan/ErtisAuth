using System;
using System.Collections.ObjectModel;
using System.Linq;
using Ertis.Core.Models.Resources;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.GeoLocation;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Dto.Extensions;
using ErtisAuth.Dto.Models.GeoLocation;
using ErtisAuth.Dto.Models.Identity;
using ErtisAuth.Dto.Models.Mailing;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Providers;
using ErtisAuth.Dto.Models.Resources;
using ErtisAuth.Dto.Models.Users;
using ErtisAuth.Dto.Models.Webhooks;
using ErtisAuth.Extensions.Mailkit.Models;
using ErtisAuth.Extensions.Mailkit.Providers;
using ErtisAuth.Extensions.Mailkit.Serialization;
using ErtisAuth.Integrations.OAuth.Core;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Extensions
{
    public static class ConverterExtensions
    {
        #region Helpers Methods

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
        
        #region Sys

        public static SysModel ToModel(this SysModelDto dto)
        {
            if (dto == null)
            {
                return null;
            }
            
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
            if (model == null)
            {
                return null;
            }
            
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

        #endregion
        
        #region User

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
        
        #region UserType
        
        public static UserType ToModel(this UserTypeDto dto)
        {
            var json = dto.Properties.ToJson();
            var properties = FieldInfoCollectionJsonConverter.Deserialize(json);

            return new UserType
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                Properties = new ReadOnlyCollection<IFieldInfo>(properties.ToList()),
                AllowAdditionalProperties = dto.AllowAdditionalProperties,
                IsAbstract = dto.IsAbstract,
                IsSealed = dto.IsSealed,
                BaseUserType = dto.BaseUserType,
                MembershipId = dto.MembershipId,
                Sys = dto.Sys.ToModel()
            };
        }
		
        public static UserTypeDto ToDto(this UserType model)
        {
            var json = FieldInfoCollectionJsonConverter.Serialize(model.Properties);
            var bsonDocument = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
            
            return new UserTypeDto
            {
                Id = model.Id,
                Name = model.Name,
                Slug = model.Slug,
                Description = model.Description,
                Properties = bsonDocument,
                AllowAdditionalProperties = model.AllowAdditionalProperties,
                IsSealed = model.IsSealed,
                IsAbstract = model.IsAbstract,
                BaseUserType = model.BaseUserType,
                MembershipId = model.MembershipId,
                Sys = model.Sys.ToDto()
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

        #region Provider

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

        #endregion
        
        #region Mailing

        public static MailHook ToModel(this MailHookDto dto)
        {
            return new MailHook
            {
                Id = dto.Id,
                Name = dto.Name,
                Slug = dto.Slug,
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
                MembershipId = model.MembershipId,
                Sys = model.Sys?.ToDto()
            };
        }

        private static Recipient ToModel(this RecipientDto dto)
        {
            return new Recipient
            {
                DisplayName = dto.DisplayName,
                EmailAddress = dto.EmailAddress
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
        
        private static IMailProvider ToMailProvider(BsonDocument bsonDocument)
        {
            return MailProviderJsonConverter.Deserialize(bsonDocument.ToJson());
        }

        #endregion

        #region ActiveToken

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

        #endregion

        #region RevokedTokens

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

        #region ClientInfo

        private static ClientInfo ToModel(this ClientInfoDto dto)
        {
            return new ClientInfo
            {
                IPAddress = dto.IPAddress,
                UserAgent = dto.UserAgent,
                GeoLocation = dto.GeoLocation != null ? new GeoLocationInfo
                {
                    City = dto.GeoLocation.City,
                    Country = dto.GeoLocation.Country,
                    CountryCode = dto.GeoLocation.CountryCode,
                    PostalCode = dto.GeoLocation.PostalCode,
                    Location = dto.GeoLocation.Location != null ? new Coordinate
                    {
                        Latitude = dto.GeoLocation.Location.Latitude,
                        Longitude = dto.GeoLocation.Location.Longitude
                    } : null,
                    Isp = dto.GeoLocation.Isp,
                    IspDomain = dto.GeoLocation.IspDomain
                } : null
            };
        }
		
        public static ClientInfoDto ToDto(this ClientInfo clientInfo)
        {
            var geoLocation = clientInfo.GeoLocation;
            GeoLocationInfoDto geoLocationDto = null;
            if (geoLocation != null)
            {
                geoLocationDto = new GeoLocationInfoDto
                {
                    City = geoLocation.City,
                    Country = geoLocation.Country,
                    CountryCode = geoLocation.CountryCode,
                    PostalCode = geoLocation.PostalCode,
                    Location = geoLocation.Location != null ? new CoordinateDto
                    {
                        Latitude = geoLocation.Location.Latitude,
                        Longitude = geoLocation.Location.Longitude
                    } : null,
                    Isp = geoLocation.Isp,
                    IspDomain = geoLocation.IspDomain
                };
            }
			
            return new ClientInfoDto
            {
                IPAddress = clientInfo.IPAddress,
                UserAgent = clientInfo.UserAgent,
                GeoLocation = geoLocationDto
            };
        }

        #endregion
    }
}