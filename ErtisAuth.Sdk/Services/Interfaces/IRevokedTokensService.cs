using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    public interface IRevokedTokensService : IReadonlyMembershipBoundedService<RevokedToken>
    {
        
    }
}