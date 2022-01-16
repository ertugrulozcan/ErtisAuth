using ErtisAuth.Core.Models.Users;
using Microsoft.AspNetCore.Html;

namespace ErtisAuth.Hub.Models
{
    public class UbacToggle
    {
        #region Properties

        public Ubac Ubac { get; }
        
        public bool IsChecked { get; }

        public DifferenceReason? ReasonOfDifference { get; init; }

        public bool IsDifferentByRole => this.ReasonOfDifference != null;
        
        public ConflictReason? ReasonOfConflict { get; init; }

        public bool IsAnyConflict => this.ReasonOfConflict != null;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ubac"></param>
        /// <param name="isChecked"></param>
        public UbacToggle(Ubac ubac, bool isChecked)
        {
            this.Ubac = ubac;
            this.IsChecked = isChecked;
        }

        #endregion

        #region Methods

        public HtmlString GetAttributes()
        {
            var ubacAttribute = $" data-ubac=\"{this.Ubac}\"";
            if (this.IsChecked)
            {
                const string checkedToggleAttributeValue = " checked=\"checked\" value=\"1\"";
                ubacAttribute += checkedToggleAttributeValue;
            }

            if (this.IsDifferentByRole)
            {
                const string differentByRoleToggleAttributeValue = " edited-for-user=\"true\"";
                ubacAttribute += differentByRoleToggleAttributeValue;
            }

            return new HtmlString(ubacAttribute);
        }

        #endregion

        #region Enums

        public enum DifferenceReason
        {
            RoleUndefinedButUserPermitted,
            RoleUndefinedButUserForbidden,
            RoleForbiddenButUserPermitted,
            RolePermittedButUserForbidden,
        }

        public enum ConflictReason
        {
            UserBothPermittedAndForbidden,
            RoleBothPermittedAndForbidden,
        }

        #endregion
    }
}