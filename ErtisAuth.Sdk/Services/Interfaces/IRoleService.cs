using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IRoleService : IMembershipBoundedService<Role>
	{
		bool CheckPermission(string rbac, TokenBase token);

		Task<bool> CheckPermissionAsync(string rbac, TokenBase token);
		
		bool CheckPermissionByRole(string roleId, string rbac, TokenBase token);

		Task<bool> CheckPermissionByRoleAsync(string roleId, string rbac, TokenBase token);
	}
}