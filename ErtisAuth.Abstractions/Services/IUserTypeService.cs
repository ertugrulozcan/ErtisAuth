using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services
{
    public interface IUserTypeService : IMembershipBoundedCrudService<UserType>
    {
        Task<UserType> GetByNameOrSlugAsync(string membershipId, string nameOrSlug, CancellationToken cancellationToken = default);
        
        Task<bool> IsInheritFromAsync(string membershipId, string childUserTypeName, string parentUserTypeName, CancellationToken cancellationToken = default);

        ValueTask<Dictionary<string, List<string>>> GetFieldInfoOwnerRelationsAsync(string membershipId, string id, CancellationToken cancellationToken = default);
    }
}