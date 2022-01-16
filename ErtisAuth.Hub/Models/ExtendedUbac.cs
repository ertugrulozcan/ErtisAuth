using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Hub.Models
{
    public class ExtendedUbac : Ubac
    {
        #region Properties

        public UbacAncestor Source { get; }

        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ubac"></param>
        /// <param name="source"></param>
        public ExtendedUbac(Ubac ubac, UbacAncestor source) : base(ubac.Resource, ubac.Action, ubac.Object)
        {
            this.Source = source;
        }

        #endregion

        #region Enums

        public enum UbacAncestor
        {
            User,
            Role
        }

        #endregion
    }
}