using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Hub.Extensions
{
    public static class LinqExtensions
    {
        #region Rbac Extensions

        public static Ubac AsUbac(this Rbac rbac)
        {
            return new Ubac(rbac.Resource, rbac.Action, rbac.Object);
        }

        #endregion
        
        #region Rbac Collection Extensions

        public static bool IsExist(
            this IEnumerable<Rbac> rbacCollection, 
            string resource, 
            RbacSegment actionSegment,
            RbacSegment objectSegment, 
            string subjectId)
        {
            return rbacCollection.Any(x =>
                x.Resource.Slug == resource &&
                x.Action.Slug == actionSegment.Slug &&
                x.Object.Slug == objectSegment.Slug &&
                (x.Subject.Slug == RbacSegment.All.Slug || x.Subject.Value == subjectId));
        }

        #endregion
        
        #region Ubac Collection Extensions

        public static bool IsExist(
            this IEnumerable<Ubac> ubacCollection, 
            string resource, 
            RbacSegment actionSegment,
            RbacSegment objectSegment)
        {
            return ubacCollection.Any(x =>
                x.Resource.Slug == resource &&
                x.Action.Slug == actionSegment.Slug &&
                x.Object.Slug == objectSegment.Slug);
        }

        #endregion
    }
}