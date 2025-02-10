using System.Linq;
using ErtisAuth.Abstractions.Services;
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

		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <returns></returns>
		public bool HasPermission(Role role, Rbac rbac)
		{
			return CheckPermission(role, rbac);
		}

		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <returns></returns>
		public bool HasPermission(Role role, string rbac)
		{
			return CheckPermission(role, Rbac.Parse(rbac));
		}

		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		public bool HasPermission(Role role, Rbac rbac, Utilizer utilizer)
		{
			return CheckPermission(role, rbac, utilizer);
		}

		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		public bool HasPermission(Role role, string rbac, Utilizer utilizer)
		{
			return CheckPermission(role, Rbac.Parse(rbac), utilizer);
		}

		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		public bool HasPermission(IUtilizer utilizer, Rbac rbac)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, rbac, utilizer);
		}

		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		public bool HasPermission(IUtilizer utilizer, string rbac)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, Rbac.Parse(rbac), utilizer);
		}

		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the owner id (ie the owner is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public bool HasPermission(IUtilizer utilizer, Rbac rbac, Utilizer owner)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, rbac, owner);
		}

		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the owner id (ie the owner is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public bool HasPermission(IUtilizer utilizer, string rbac, Utilizer owner)
		{
			return this.CheckPermission(utilizer.Role, utilizer.MembershipId, Rbac.Parse(rbac), owner);
		}

		private bool CheckPermission(string roleSlug, string membershipId, Rbac rbac, IUtilizer utilizer = null)
		{
			var hasUbacPermission = utilizer?.HasPermission(rbac);
			if (hasUbacPermission != null)
			{
				return hasUbacPermission.Value;
			}
			
			var role = this.roleService.GetBySlug(roleSlug, membershipId);
			if (role != null)
			{
				return CheckPermission(role, rbac, utilizer);
			}
			else
			{
				throw Core.Exceptions.ErtisAuthException.RoleNotFound(roleSlug, true);
			}
		}
		
		private bool CheckPermission(string roleSlug, string membershipId, Rbac rbac, Utilizer utilizer)
		{
			var hasUbacPermission = utilizer.HasPermission(rbac);
			if (hasUbacPermission != null)
			{
				return hasUbacPermission.Value;
			}
			
			var role = this.roleService.GetBySlug(roleSlug, membershipId);
			if (role != null)
			{
				return CheckPermission(role, rbac, utilizer);
			}
			else
			{
				throw Core.Exceptions.ErtisAuthException.RoleNotFound(roleSlug, true);
			}
		}
		
		private static bool CheckPermission(Role role, Rbac rbac, IUtilizer utilizer = null)
		{
			var hasUbacPermission = utilizer?.HasPermission(rbac);
			if (hasUbacPermission != null)
			{
				return hasUbacPermission.Value;
			}
			
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
			var hasUbacPermission = utilizer.HasPermission(rbac);
			if (hasUbacPermission != null)
			{
				return hasUbacPermission.Value;
			}
			
			if (role.HasPermission(rbac))
			{
				if (utilizer.Scopes != null && utilizer.Scopes.Any(x => !string.IsNullOrWhiteSpace(x)))
				{
					return utilizer.Scopes.HasPermission(rbac);
				}
				else
				{
					return true;
				}
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