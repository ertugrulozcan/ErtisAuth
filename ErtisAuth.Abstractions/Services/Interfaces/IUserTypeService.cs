using System.Threading.Tasks;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
    public interface IUserTypeService : IMembershipBoundedCrudService<UserType>
    {
        Task<UserType> GetByNameOrSlugAsync(string membershipId, string nameOrSlug);
        
        Task<bool> IsInheritFromAsync(string membershipId, string childUserTypeName, string parentUserTypeName);
    }
}