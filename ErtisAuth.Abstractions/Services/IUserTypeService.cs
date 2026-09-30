using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

public interface IUserTypeService : IMembershipBoundedCrudService<UserType>
{
	Task<UserType?> GetByNameOrSlugAsync(string nameOrSlug, string membershipId, bool forceGetFreshData = false, CancellationToken cancellationToken = default);
	
	Task<bool> IsInheritFromAsync(string childUserTypeName, string parentUserTypeName, string membershipId, CancellationToken cancellationToken = default);
	
	Task<Dictionary<string, List<string>>?> GetFieldInfoOwnerRelationsAsync(string id, string membershipId, CancellationToken cancellationToken = default);
}