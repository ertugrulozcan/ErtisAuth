using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

public interface IUserTypeService : IMembershipBoundedCrudService<UserType>
{
	Task<UserType?> GetBySlugAsync(string slug, string membershipId, bool forceGetFreshData = false, CancellationToken cancellationToken = default);
	
	Task<bool> IsInheritFromAsync(string childUserTypeSlug, string parentUserTypeSlug, string membershipId, CancellationToken cancellationToken = default);
	
	Task<Dictionary<string, List<string>>?> GetFieldInfoOwnerRelationsAsync(string id, string membershipId, CancellationToken cancellationToken = default);
}