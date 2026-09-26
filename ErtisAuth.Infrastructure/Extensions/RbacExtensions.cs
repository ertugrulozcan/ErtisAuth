using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Infrastructure.Extensions;

public static class RbacExtensions
{
	#region Methods
	
	public static bool HasPermission(this Role role, Rbac rbac)
	{
		return !role.IsForbidden(rbac) && (role.Permissions?.Any(x => IsMatch(x, rbac)) ?? false);
	}
	
	public static bool IsForbidden(this Role role, Rbac rbac)
	{
		return role.Forbidden?.Any(x => IsMatch(x, rbac)) ?? false;
	}
	
	/// <summary>
	/// Returns whether any of the given token scopes covers the rbac.
	/// Scopes use the same format ([subject].[resource].[action].[object], 1 to 4 segments) and the same matching as role permissions.
	/// </summary>
	public static bool CoversScope(this IEnumerable<string>? scopes, Rbac rbac)
	{
		return scopes?.Any(x => IsMatch(x, rbac)) ?? false;
	}
	
	private static bool IsMatch(string permission, Rbac rbac)
	{
		if (Rbac.TryParse(permission, out var roleRbac) && roleRbac != null)
		{
			var isSubjectPermitted = roleRbac.Subject.IsAll() || roleRbac.Subject.Equals(rbac.Subject);
			var isResourcePermitted = roleRbac.Resource.IsAll() || roleRbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
			var isActionPermitted = roleRbac.Action.IsAll() || roleRbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
			var isObjectPermitted = roleRbac.Object.IsAll() || roleRbac.Object.Equals(rbac.Object);
			
			return isSubjectPermitted && isResourcePermitted && isActionPermitted && isObjectPermitted;
		}
		
		return false;
	}
	
	public static bool HasOwnUpdatePermission(this Role _, Rbac rbac, Utilizer utilizer)
	{
		if (rbac.Action.Slug != Rbac.GetSegment(Rbac.CrudActions.Update).Slug)
		{
			return false;
		}
		
		if (rbac.Resource== "users" && utilizer.Type == Utilizer.UtilizerType.User)
		{
			if (rbac.Object == utilizer.Id)
			{
				return true;
			}
		}
		
		if (rbac.Resource== "applications" && utilizer.Type == Utilizer.UtilizerType.Application)
		{
			if (rbac.Object == utilizer.Id)
			{
				return true;
			}
		}
		
		return false;
	}
	
	public static bool HasOwnUpdatePermission(this Role _, Rbac rbac, IUtilizer utilizer)
	{
		if (rbac.Action.Slug != Rbac.GetSegment(Rbac.CrudActions.Update).Slug)
		{
			return false;
		}
		
		if (rbac.Resource== "users" && utilizer.UtilizerType == Utilizer.UtilizerType.User)
		{
			if (rbac.Object == utilizer.Id)
			{
				return true;
			}
		}
		
		if (rbac.Resource== "applications" && utilizer.UtilizerType == Utilizer.UtilizerType.Application)
		{
			if (rbac.Object == utilizer.Id)
			{
				return true;
			}
		}
		
		return false;
	}
	
	#endregion
}