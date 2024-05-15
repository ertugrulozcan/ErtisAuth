using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Abstractions.Services
{
	public interface IAccessControlService
	{
		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <returns></returns>
		bool HasPermission(Role role, Rbac rbac);
		
		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <returns></returns>
		bool HasPermission(Role role, string rbac);
		
		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		bool HasPermission(Role role, Rbac rbac, Utilizer utilizer);
		
		/// <summary>
		/// Returns whether the given role has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		bool HasPermission(Role role, string rbac, Utilizer utilizer);
		
		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		bool HasPermission(IUtilizer utilizer, Rbac rbac);
		
		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <returns></returns>
		bool HasPermission(IUtilizer utilizer, string rbac);
		
		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the owner id (ie the owner is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		bool HasPermission(IUtilizer utilizer, Rbac rbac, Utilizer owner);
		
		/// <summary>
		/// Returns whether the role of utilizer has the permission specified in the given rbac expression. Also if the rbac action is 'update' and the rcab object is equal to the owner id (ie the owner is the user doing the action) accepted to be permitted.
		/// </summary>
		/// <param name="rbac"></param>
		/// <param name="utilizer"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		bool HasPermission(IUtilizer utilizer, string rbac, Utilizer owner);
	}
}