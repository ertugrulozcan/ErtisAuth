using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IRoleService
	{
		bool CheckPermission(string rbac, TokenBase token);

		Task<bool> CheckPermissionAsync(string rbac, TokenBase token);
	}
}