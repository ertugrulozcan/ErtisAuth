using Ertis.Core.Models.Resources;
using ErtisAuth.Dto.Models.Resources;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class SysExtensions
{
    #region Methods

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
}