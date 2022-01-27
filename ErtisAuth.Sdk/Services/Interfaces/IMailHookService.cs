using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    public interface IMailHookService : IMembershipBoundedService<MailHook>
    {
        
    }
}