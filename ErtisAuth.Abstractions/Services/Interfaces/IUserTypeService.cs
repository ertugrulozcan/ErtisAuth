using System.Collections.Generic;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
    public interface IUserTypeService : IMembershipBoundedCrudService<UserType>
    {
        Task<UserType> GetByNameOrSlugAsync(string membershipId, string nameOrSlug);
        
        Task<bool> IsInheritFromAsync(string membershipId, string childUserTypeName, string parentUserTypeName);

        ValueTask<Dictionary<string, List<string>>> GetFieldInfoOwnerRelationsAsync(string membershipId, string id);
    }
}