using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Infrastructure.Extensions;

namespace ErtisAuth.Infrastructure.Services
{
	public class AccessControlService : IAccessControlService
	{
		#region Services

		private readonly IRoleService roleService;

		#endregion
		
		#region Constructors

		public AccessControlService(IRoleService roleService)
		{
			this.roleService = roleService;
		}

		#endregion
		
		#region Methods

		public bool HasPermission(Role role, Rbac rbac)
		{
			return CheckPermission(role, rbac);
		}

		public bool HasPermission(Role role, string rbac)
		{
			return CheckPermission(role, Rbac.Parse(rbac));
		}

		public bool HasPermission(Role role, Rbac rbac, Utilizer utilizer)
		{
			return CheckPermission(role, rbac, utilizer);
		}

		public bool HasPermission(Role role, string rbac, Utilizer utilizer)
		{
			return CheckPermission(role, Rbac.Parse(rbac), utilizer);
		}

		public bool HasPermission(IUtilizer utilizer, Rbac rbac)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, rbac, utilizer);
		}

		public bool HasPermission(IUtilizer utilizer, string rbac)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, Rbac.Parse(rbac), utilizer);
		}

		public bool HasPermission(IUtilizer utilizer, Rbac rbac, Utilizer owner)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, rbac, owner);
		}

		public bool HasPermission(IUtilizer utilizer, string rbac, Utilizer owner)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, Rbac.Parse(rbac), owner);
		}

		private bool CheckPermission(string roleName, string membershipId, Rbac rbac, IUtilizer utilizer = null)
		{
			var role = this.roleService.GetByName(roleName, membershipId);
			if (role != null)
			{
				return CheckPermission(role, rbac, utilizer);
			}
			else
			{
				throw Core.Exceptions.ErtisAuthException.RoleNotFound(roleName, true);
			}
		}
		
		private bool CheckPermission(string roleName, string membershipId, Rbac rbac, Utilizer utilizer)
		{
			var role = this.roleService.GetByName(roleName, membershipId);
			if (role != null)
			{
				return CheckPermission(role, rbac, utilizer);
			}
			else
			{
				throw Core.Exceptions.ErtisAuthException.RoleNotFound(roleName, true);
			}
		}
		
		private static bool CheckPermission(Role role, Rbac rbac, IUtilizer utilizer = null)
		{
			if (role.HasPermission(rbac))
			{
				return true;
			}
			else if (utilizer != null && role.HasOwnUpdatePermission(rbac, utilizer))
			{
				return true;
			}
			else
			{
				return false;
			}	
		}
		
		private static bool CheckPermission(Role role, Rbac rbac, Utilizer utilizer)
		{
			if (role.HasPermission(rbac))
			{
				return true;
			}
			else if (role.HasOwnUpdatePermission(rbac, utilizer))
			{
				return true;
			}
			else
			{
				return false;
			}	
		}

		#endregion
	}
}