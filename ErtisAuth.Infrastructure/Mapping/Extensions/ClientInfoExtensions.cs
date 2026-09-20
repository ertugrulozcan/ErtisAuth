using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Infrastructure.Mapping.Extensions;

public static class ClientInfoExtensions
{
    #region Methods
    
    public static ClientInfo ToModel(this ClientInfoDto dto)
    {
        return new ClientInfo
        {
            IPAddress = dto.IPAddress,
            UserAgent = dto.UserAgent
        };
    }
	
    public static ClientInfoDto ToDto(this ClientInfo clientInfo)
    {
        return new ClientInfoDto
        {
            IPAddress = clientInfo.IPAddress,
            UserAgent = clientInfo.UserAgent
        };
    }
    
    #endregion
}