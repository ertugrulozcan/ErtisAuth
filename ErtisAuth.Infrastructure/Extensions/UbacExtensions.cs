using System;
using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Infrastructure.Extensions
{
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
				if (Ubac.TryParse(permission, out var userUbac))
				{
					var isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
					var isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
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
				if (Ubac.TryParse(permission, out var userUbac))
				{
					var isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
					var isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
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
		
		public static bool HasPermission(this IEnumerable<string> scopes, Rbac rbac)
		{
			var matchedPermissions = scopes?.Where(isPermittedFilter) ?? Array.Empty<string>();
			var permissions = matchedPermissions as string[] ?? matchedPermissions.ToArray();
			return permissions.Any();

			bool isPermittedFilter(string permission)
			{
				if (Ubac.TryParse(permission, out var userUbac))
				{
					var isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
					var isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
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

		#endregion
	}
}