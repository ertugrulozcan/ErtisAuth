using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Abstractions.Services
{
    public interface IMailHookService : IMembershipBoundedCrudService<MailHook>
    {
        void SendHookMailAsync(
            MailHook mailHook, 
            string userId, 
            string membershipId, 
            object payload,
            CancellationToken cancellationToken = default);

        Task<MailHook> GetUserActivationMailHookAsync(string membershipId, CancellationToken cancellationToken = default);
        
        Task<MailHook> GetResetPasswordMailHookAsync(string membershipId, CancellationToken cancellationToken = default);
    }
}