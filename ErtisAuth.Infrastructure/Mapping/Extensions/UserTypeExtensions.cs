using System.Collections.ObjectModel;
using System.Linq;
using Ertis.Schema.Serialization;
using Ertis.Schema.Types;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dto.Models.Users;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class UserTypeExtensions
{
    #region Methods

    public static UserType ToModel(this UserTypeDto dto)
    {
        var json = dto.Properties.ToJson();
        var properties = FieldInfoCollectionJsonConverter.Deserialize(json);

        return new UserType
        {
            Id = dto.Id,
            Name = dto.Name,
            Slug = dto.Slug,
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
}