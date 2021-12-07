using System.Threading.Tasks;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IRoleService : IMembershipBoundedCrudService<Role>
	{
		Role GetByName(string name, string membershipId);
		
		ValueTask<Role> GetByNameAsync(string name, string membershipId);
	}
}