using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Infrastructure.Extensions;

namespace ErtisAuth.Infrastructure.Services;

public class AccessControlService : IAccessControlService
{
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
	/// Returns whether the given role has the permission specified in the given rbac expression, also if the rbac action is 'update' and the rbac object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
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
	/// Returns whether the given role has the permission specified in the given rbac expression, also if the rbac action is 'update' and the rbac object is equal to the utilizer id (ie the utilizer is the user doing the action) accepted to be permitted.
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
	/// Returns whether the given role or the utilizer's own permissions (UBAC) grant the permission specified in the given rbac expression.
	/// Unlike HasPermission, the own-update exception (a user updating itself) is not taken into account,
	/// so this is the check for changes that require a real update permission (e.g. role, permissions, forbidden).
	/// </summary>
	/// <param name="role"></param>
	/// <param name="rbac"></param>
	/// <param name="utilizer"></param>
	/// <returns></returns>
	public bool HasGrantedPermission(Role? role, Rbac rbac, Utilizer utilizer)
	{
		return Evaluate(role, rbac, utilizer.HasPermission(rbac), () => false, utilizer.Scopes);
	}
	
	private static bool CheckPermission(Role role, Rbac rbac, IUtilizer? utilizer = null)
	{
		return Evaluate(role, rbac, utilizer?.HasPermission(rbac), () => utilizer != null && role.HasOwnUpdatePermission(rbac, utilizer), null);
	}
	
	private static bool CheckPermission(Role role, Rbac rbac, Utilizer utilizer)
	{
		return Evaluate(role, rbac, utilizer.HasPermission(rbac), () => role.HasOwnUpdatePermission(rbac, utilizer) || role.HasOwnReadPermission(rbac, utilizer), utilizer.Scopes);
	}
	
	/// <summary>
	/// The single authorization decision:
	/// 1. A matching UBAC entry (user permissions/forbidden) is decisive,
	/// 2. otherwise the role permissions (role forbidden wins),
	/// 3. otherwise the own record exceptions (a user or an application updating itself, an application reading itself),
	///    unless the role forbids the action,
	/// and finally the token scopes (if any) must cover the request, whichever rule granted it.
	/// </summary>
	private static bool Evaluate(Role? role, Rbac rbac, bool? ubacDecision, Func<bool> isOwnUpdate, IEnumerable<string>? scopes)
	{
		bool isPermitted;
		if (ubacDecision != null)
		{
			isPermitted = ubacDecision.Value;
		}
		else if (role == null)
		{
			isPermitted = false;
		}
		else
		{
			isPermitted = role.HasPermission(rbac) || (!role.IsForbidden(rbac) && isOwnUpdate());
		}
		
		var scopeList = scopes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
		if (isPermitted && scopeList is { Length: > 0 })
		{
			return scopeList.CoversScope(rbac);
		}
		
		return isPermitted;
	}
	
	#endregion
}