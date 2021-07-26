using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IAccessControlService
	{
		bool HasPermission(Role role, Rbac rbac);
		
		bool HasPermission(Role role, string rbac);
		
		bool HasPermission(Role role, Rbac rbac, Utilizer utilizer);
		
		bool HasPermission(Role role, string rbac, Utilizer utilizer);
		
		bool HasPermission(IUtilizer utilizer, Rbac rbac);
		
		bool HasPermission(IUtilizer utilizer, string rbac);
		
		bool HasPermission(IUtilizer utilizer, Rbac rbac, Utilizer owner);
		
		bool HasPermission(IUtilizer utilizer, string rbac, Utilizer owner);
	}
}