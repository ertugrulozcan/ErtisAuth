using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IRoleService
	{
		bool CheckPermission(string rbac, TokenBase token);

		Task<bool> CheckPermissionAsync(string rbac, TokenBase token);
		
		bool CheckPermissionByRole(string roleId, string rbac, TokenBase token);

		Task<bool> CheckPermissionByRoleAsync(string roleId, string rbac, TokenBase token);
		
		IResponseResult<Role> CreateRole(Role role, TokenBase token);
		
		Task<IResponseResult<Role>> CreateRoleAsync(Role role, TokenBase token);
		
		IResponseResult<Role> GetRole(string roleId, TokenBase token);
		
		Task<IResponseResult<Role>> GetRoleAsync(string roleId, TokenBase token);
		
		IResponseResult<IPaginationCollection<Role>> GetRoles(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<Role>>> GetRolesAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		IResponseResult<IPaginationCollection<Role>> QueryRoles(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<Role>>> QueryRolesAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		IResponseResult<Role> UpdateRole(Role role, TokenBase token);
		
		Task<IResponseResult<Role>> UpdateRoleAsync(Role role, TokenBase token);
		
		IResponseResult DeleteRole(string roleId, TokenBase token);
		
		Task<IResponseResult> DeleteRoleAsync(string roleId, TokenBase token);
	}
}