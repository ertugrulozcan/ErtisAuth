using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services
{
    public interface IDeletableMembershipBoundedService
    {
        bool Delete(Utilizer utilizer, string membershipId, string id);

        ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default);

        bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids);

        ValueTask<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids, CancellationToken cancellationToken = default);
    }
}