using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Hub.Helpers
{
    public static class UserPermissionsHelper
    {
        #region Methods

        public static UbacTable MergePermissionsAndForbiddens(
	        string userId,
	        IEnumerable<string> userPermissions,
	        IEnumerable<string> userForbidden,
	        IEnumerable<string> rolePermissions,
	        IEnumerable<string> roleForbidden)
        {
	        var outPermissionList = new List<ExtendedUbac>();
	        var outForbiddenList = new List<ExtendedUbac>();

	        var ubacRows = MergePermissionsAndForbiddens(
		        userId,
		        userPermissions,
		        userForbidden,
		        rolePermissions,
		        roleForbidden,
		        outPermissionList,
		        outForbiddenList);

	        return new UbacTable
	        {
		        Rows = ubacRows,
		        MergedPermissions = outPermissionList,
		        MergedForbiddens = outForbiddenList
	        };
        }
        
        private static IEnumerable<UbacRow> MergePermissionsAndForbiddens(
			string userId,
			IEnumerable<string> userPermissions,
			IEnumerable<string> userForbidden,
			IEnumerable<string> rolePermissions,
			IEnumerable<string> roleForbidden,
			ICollection<ExtendedUbac> outPermissionList,
			ICollection<ExtendedUbac> outForbiddenList)
		{
			string[] predefinedResources = 
			{
				"users",
				"applications",
				"roles",
				"memberships",
				"events",
				"providers",
				"webhooks",
				"tokens"
			};
			
			var userPermissionUbacs = userPermissions != null ? userPermissions.Select(Ubac.Parse).ToList() : new List<Ubac>();
			var userForbiddenUbacs = userForbidden != null ? userForbidden.Select(Ubac.Parse).ToList() : new List<Ubac>();
			var rolePermissionRbacs = rolePermissions != null ? rolePermissions.Select(Rbac.Parse).ToList() : new List<Rbac>();
			var roleForbiddenRbacs = roleForbidden != null ? roleForbidden.Select(Rbac.Parse).ToList() : new List<Rbac>();

			var allResourceNames = new List<string>();
			allResourceNames.AddRange(predefinedResources);
			allResourceNames.AddRange(userPermissionUbacs.Select(x => x.Resource.Slug));
			allResourceNames.AddRange(userForbiddenUbacs.Select(x => x.Resource.Slug));
			allResourceNames.AddRange(rolePermissionRbacs.Select(x => x.Resource.Slug));
			allResourceNames.AddRange(roleForbiddenRbacs.Select(x => x.Resource.Slug));
			allResourceNames = allResourceNames.Distinct().ToList();

			foreach (var resourceName in allResourceNames)
			{
				var ubacRow = new UbacRow
				{
					ResourceName = resourceName,
					CreateToggle = GetUbacToggle(resourceName, Rbac.CrudActionSegments.Create, userId, userPermissionUbacs, userForbiddenUbacs, rolePermissionRbacs, roleForbiddenRbacs, outPermissionList, outForbiddenList),
					ReadToggle = GetUbacToggle(resourceName, Rbac.CrudActionSegments.Read, userId, userPermissionUbacs, userForbiddenUbacs, rolePermissionRbacs, roleForbiddenRbacs, outPermissionList, outForbiddenList),
					UpdateToggle = GetUbacToggle(resourceName, Rbac.CrudActionSegments.Update, userId, userPermissionUbacs, userForbiddenUbacs, rolePermissionRbacs, roleForbiddenRbacs, outPermissionList, outForbiddenList),
					DeleteToggle = GetUbacToggle(resourceName, Rbac.CrudActionSegments.Delete, userId, userPermissionUbacs, userForbiddenUbacs, rolePermissionRbacs, roleForbiddenRbacs, outPermissionList, outForbiddenList)
				};

				yield return ubacRow;
			}
		}

		private static UbacToggle GetUbacToggle(
			string resourceName, 
			RbacSegment actionSegment,
			string userId, 
			IEnumerable<Ubac> userPermissionUbacs, 
			IEnumerable<Ubac> userForbiddenUbacs, 
			IEnumerable<Rbac> rolePermissionRbacs, 
			IEnumerable<Rbac> roleForbiddenRbacs,
			ICollection<ExtendedUbac> outPermissionList,
			ICollection<ExtendedUbac> outForbiddenList)
		{
			UbacToggle.ConflictReason? reasonOfConflict = null;
			var isExistInUserPermissions = userPermissionUbacs.IsExist(resourceName, actionSegment, RbacSegment.All);
			var isExistInUserForbiddens = userForbiddenUbacs.IsExist(resourceName, actionSegment, RbacSegment.All);
			if (isExistInUserPermissions && isExistInUserForbiddens)
			{
				// CONFLICT !!!
				reasonOfConflict = UbacToggle.ConflictReason.UserBothPermittedAndForbidden;
			}
			
			var isExistInRolePermissions = rolePermissionRbacs.IsExist(resourceName, actionSegment, RbacSegment.All, userId);
			var isExistInRoleForbiddens = roleForbiddenRbacs.IsExist(resourceName, actionSegment, RbacSegment.All, userId);
			if (isExistInRolePermissions && isExistInRoleForbiddens)
			{
				// CONFLICT !!!
				reasonOfConflict = UbacToggle.ConflictReason.RoleBothPermittedAndForbidden;
			}

			var isPermitted = isExistInUserPermissions || isExistInRolePermissions;
			var isForbidden = isExistInUserForbiddens || isExistInRoleForbiddens;
			
			var isChecked = !isForbidden && isPermitted;
			var isDifferentByRole = isExistInUserPermissions != isExistInRolePermissions || isExistInUserForbiddens != isExistInRoleForbiddens;
			var isRegularDifferency =
				isDifferentByRole &&
				isExistInRolePermissions && !isExistInUserPermissions &&
				isExistInRoleForbiddens && !isExistInUserForbiddens;
			
			UbacToggle.DifferenceReason? reasonOfDifference = null;
			if (isDifferentByRole && !isRegularDifferency)
			{
				if (isExistInUserPermissions && !isExistInRolePermissions)
				{
					reasonOfDifference = isExistInRoleForbiddens ? 
						UbacToggle.DifferenceReason.RoleForbiddenButUserPermitted : 
						UbacToggle.DifferenceReason.RoleUndefinedButUserPermitted;
				}
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (isExistInUserForbiddens && !isExistInRoleForbiddens)
				{
					reasonOfDifference = isExistInRolePermissions ? 
						UbacToggle.DifferenceReason.RolePermittedButUserForbidden : 
						UbacToggle.DifferenceReason.RoleUndefinedButUserForbidden;
				}
			}

			var ubac = new Ubac(new RbacSegment(resourceName), actionSegment, RbacSegment.All);
			var ubacToggle = new UbacToggle(ubac, isChecked)
			{
				ReasonOfDifference = reasonOfDifference,
				ReasonOfConflict = reasonOfConflict
			};

			if (outPermissionList != null)
			{
				if (isExistInUserPermissions)
				{
					outPermissionList.Add(new ExtendedUbac(ubac, ExtendedUbac.UbacAncestor.User));
				}
				else if (isExistInRolePermissions)
				{
					outPermissionList.Add(new ExtendedUbac(ubac, ExtendedUbac.UbacAncestor.Role));
				}
			}

			if (outForbiddenList != null)
			{
				if (isExistInUserForbiddens)
				{
					outForbiddenList.Add(new ExtendedUbac(ubac, ExtendedUbac.UbacAncestor.User));
				}
				else if (isExistInRoleForbiddens)
				{
					outForbiddenList.Add(new ExtendedUbac(ubac, ExtendedUbac.UbacAncestor.Role));
				}	
			}

			return ubacToggle;
		}

        #endregion
    }
}