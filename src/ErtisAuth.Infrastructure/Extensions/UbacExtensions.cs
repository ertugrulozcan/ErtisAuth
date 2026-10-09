using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Infrastructure.Extensions;

public static class UbacExtensions
{
	#region Methods
	
	public static bool? HasPermission(this IUtilizer utilizer, Rbac rbac)
	{
		var matchedPermissions = utilizer.Permissions?.Where(isPermittedFilter) ?? Array.Empty<string>();
		var matchedForbiddens = utilizer.Forbidden?.Where(isPermittedFilter) ?? Array.Empty<string>();
		
		var permissions = matchedPermissions as string[] ?? matchedPermissions.ToArray();
		var forbiddens = matchedForbiddens as string[] ?? matchedForbiddens.ToArray();
		
		if (!permissions.Any() && !forbiddens.Any())
		{
			return null;
		}
		
		return !forbiddens.Any() && permissions.Any();
		
		bool isPermittedFilter(string permission)
		{
			if (Ubac.TryParse(permission, out var userUbac) && userUbac != null)
			{
				var isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, StringComparison.InvariantCultureIgnoreCase);
				var isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, StringComparison.InvariantCultureIgnoreCase);
				var isObjectPermitted = userUbac.Object.IsAll() || userUbac.Object.Equals(rbac.Object);
				
				var isPermitted = isResourcePermitted && isActionPermitted && isObjectPermitted;
				
				if (isPermitted)
				{
					return true;
				}
			}
			
			return false;
		}
	}
	
	public static bool? HasPermission(this Utilizer utilizer, Rbac rbac)
	{
		var matchedPermissions = utilizer.Permissions?.Where(isPermittedFilter) ?? Array.Empty<string>();
		var matchedForbiddens = utilizer.Forbidden?.Where(isPermittedFilter) ?? Array.Empty<string>();
		
		var permissions = matchedPermissions as string[] ?? matchedPermissions.ToArray();
		var forbiddens = matchedForbiddens as string[] ?? matchedForbiddens.ToArray();
		
		if (!permissions.Any() && !forbiddens.Any())
		{
			return null;
		}
		
		return !forbiddens.Any() && permissions.Any();
		
		bool isPermittedFilter(string permission)
		{
			if (Ubac.TryParse(permission, out var userUbac) && userUbac != null)
			{
				var isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, Rbac.NameSegmentComparison);
				var isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, Rbac.NameSegmentComparison);
				var isObjectPermitted = userUbac.Object.IsAll() || userUbac.Object.Equals(rbac.Object);
				
				var isPermitted = isResourcePermitted && isActionPermitted && isObjectPermitted;
				
				if (isPermitted)
				{
					return true;
				}
			}
			
			return false;
		}
	}
	
	public static bool HasConflict(IEnumerable<string>? permissions, IEnumerable<string>? forbiddens, out Ubac? conflictedUbac)
	{
		var permissionList = new List<Ubac>();
		if (permissions != null)
		{
			foreach (var permission in permissions)
			{
				var ubac = Ubac.Parse(permission);
				permissionList.Add(ubac);
			}
		}
		
		var forbiddenList = new List<Ubac>();
		if (forbiddens != null)
		{
			foreach (var forbidden in forbiddens)
			{
				var ubac = Ubac.Parse(forbidden);
				forbiddenList.Add(ubac);
			}
		}
		
		// Is there any conflict?
		foreach (var permissionUbac in permissionList)
		{
			foreach (var forbiddenUbac in forbiddenList)
			{
				if (permissionUbac == forbiddenUbac)
				{
					conflictedUbac = permissionUbac;
					return true;
				}
			}
		}
		
		conflictedUbac = null;
		return false;
	}
	
	#endregion
}