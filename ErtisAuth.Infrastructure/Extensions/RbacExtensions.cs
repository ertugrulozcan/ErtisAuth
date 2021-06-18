using System;
using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Infrastructure.Extensions
{
	public static class RbacExtensions
	{
		#region Methods

		public static bool HasPermission(this Role role, Rbac rbac)
		{
			bool isPermittedFilter(string permission)
			{
				if (Rbac.TryParse(permission, out var userRbac))
				{
					bool isSubjectPermitted = userRbac.Subject.IsAll() || userRbac.Subject.Equals(rbac.Subject);
					bool isResourcePermitted = userRbac.Resource.IsAll() || userRbac.Resource.Equals(rbac.Resource, StringComparison.CurrentCultureIgnoreCase);
					bool isActionPermitted = userRbac.Action.IsAll() || userRbac.Action.Equals(rbac.Action, StringComparison.CurrentCultureIgnoreCase);
					bool isObjectPermitted = userRbac.Object.IsAll() || userRbac.Object.Equals(rbac.Object);

					bool isPermitted = isSubjectPermitted && isResourcePermitted && isActionPermitted && isObjectPermitted;

					if (isPermitted)
					{
						return true;
					}
				}

				return false;
			}

			var matchedPermissions = role.Permissions?.Where(isPermittedFilter) ?? new string[] {};
			var matchedForbiddens = role.Forbidden?.Where(isPermittedFilter) ?? new string[] {};

			return !matchedForbiddens.Any() && matchedPermissions.Any();
		}

		public static bool HasOwnUpdatePermission(this Role role, Rbac rbac, Utilizer utilizer)
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

		#endregion
	}
}