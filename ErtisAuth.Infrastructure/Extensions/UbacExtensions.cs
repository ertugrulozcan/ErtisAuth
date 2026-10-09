using System;
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
			bool isPermittedFilter(string permission)
			{
				if (Ubac.TryParse(permission, out var userUbac))
				{
					bool isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
					bool isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
					bool isObjectPermitted = userUbac.Object.IsAll() || userUbac.Object.Equals(rbac.Object);

					bool isPermitted = isResourcePermitted && isActionPermitted && isObjectPermitted;

					if (isPermitted)
					{
						return true;
					}
				}

				return false;
			}

			var matchedPermissions = utilizer.Permissions?.Where(isPermittedFilter) ?? new string[] {};
			var matchedForbiddens = utilizer.Forbidden?.Where(isPermittedFilter) ?? new string[] {};

			var permissions = matchedPermissions as string[] ?? matchedPermissions.ToArray();
			var forbiddens = matchedForbiddens as string[] ?? matchedForbiddens.ToArray();
			
			if (!permissions.Any() && !forbiddens.Any())
			{
				return null;
			}
			
			return !forbiddens.Any() && permissions.Any();
		}
		
		public static bool? HasPermission(this Utilizer utilizer, Rbac rbac)
		{
			bool isPermittedFilter(string permission)
			{
				if (Ubac.TryParse(permission, out var userUbac))
				{
					bool isResourcePermitted = userUbac.Resource.IsAll() || userUbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
					bool isActionPermitted = userUbac.Action.IsAll() || userUbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
					bool isObjectPermitted = userUbac.Object.IsAll() || userUbac.Object.Equals(rbac.Object);

					bool isPermitted = isResourcePermitted && isActionPermitted && isObjectPermitted;

					if (isPermitted)
					{
						return true;
					}
				}

				return false;
			}

			var matchedPermissions = utilizer.Permissions?.Where(isPermittedFilter) ?? new string[] {};
			var matchedForbiddens = utilizer.Forbidden?.Where(isPermittedFilter) ?? new string[] {};

			var permissions = matchedPermissions as string[] ?? matchedPermissions.ToArray();
			var forbiddens = matchedForbiddens as string[] ?? matchedForbiddens.ToArray();
			
			if (!permissions.Any() && !forbiddens.Any())
			{
				return null;
			}
			
			return !forbiddens.Any() && permissions.Any();
		}

		#endregion
	}
}