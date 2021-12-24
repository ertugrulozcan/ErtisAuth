using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Extensions;
using ErtisAuth.Dto.Models.Memberships;
using ErtisAuth.Dto.Models.Resources;
using ErtisAuth.Dto.Models.Users;
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
                ExpiresIn = dto.ExpiresIn,
                SecretKey = dto.SecretKey,
                RefreshTokenExpiresIn = dto.RefreshTokenExpiresIn,
                UserType = dto.UserType?.ToModel(),
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
                ExpiresIn = model.ExpiresIn,
                SecretKey = model.SecretKey,
                RefreshTokenExpiresIn = model.RefreshTokenExpiresIn,
                UserType = model.UserType?.ToDto(),
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
    }
}