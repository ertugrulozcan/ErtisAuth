using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
    public interface IDeletableMembershipBoundedService
    {
        bool Delete(Utilizer utilizer, string membershipId, string id);

        ValueTask<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id);

        bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids);

        ValueTask<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids);
    }
}