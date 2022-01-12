using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    public interface IRevokedTokensService : IReadonlyMembershipBoundedService<RevokedToken>
    {
        
    }
}