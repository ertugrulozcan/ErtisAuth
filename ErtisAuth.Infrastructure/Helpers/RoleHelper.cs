using System.Collections.Generic;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Infrastructure.Helpers
{
	internal static class RoleHelper
	{
		#region Methods

		internal static IEnumerable<string> AssertAdminPermissionsForReservedResources()
		{
			string[] reservedResources = {
				"memberships",
				"users",
				"user-types",
				"applications",
				"roles",
				"sessions",
				"events",
				"providers",
				"tokens",
				"webhooks",
				"mailhooks",
			};
			
			RbacSegment[] adminPrivileges =
			{
				Rbac.CrudActionSegments.Create,
				Rbac.CrudActionSegments.Read,
				Rbac.CrudActionSegments.Update,
				Rbac.CrudActionSegments.Delete
			};

			var permissions = new List<string>();
			foreach (var resource in reservedResources)
			{
				var resourceSegment = new RbacSegment(resource);
				foreach (var privilege in adminPrivileges)
				{
					var rbac = new Rbac(RbacSegment.All, resourceSegment, privilege, RbacSegment.All);
					permissions.Add(rbac.ToString());	
				}
			}

			return permissions;
		}

		#endregion
	}
}