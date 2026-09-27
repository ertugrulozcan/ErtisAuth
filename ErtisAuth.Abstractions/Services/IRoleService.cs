using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Abstractions.Services;

public interface IRoleService : IMembershipBoundedCrudService<Role>
{
	Role? GetBySlug(string slug, string membershipId);
	
	ValueTask<Role?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Returns the administrator role of the membership, creating it when it does not exist.
	/// </summary>
	Task<Role> EnsureAdministratorRoleAsync(Membership membership, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Ensures the administrator role of every membership; called once at startup.
	/// </summary>
	Task EnsureAdministratorRolesAsync(CancellationToken cancellationToken = default);
}