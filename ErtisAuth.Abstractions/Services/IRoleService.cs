using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Abstractions.Services
{
	public interface IRoleService : IMembershipBoundedCrudService<Role>
	{
		Role GetBySlug(string slug, string membershipId);
		
		ValueTask<Role> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
	}
}