using System.Threading.Tasks;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IRoleService : IMembershipBoundedCrudService<Role>
	{
		Task<Role> GetByNameAsync(string name, string membershipId);
	}
}