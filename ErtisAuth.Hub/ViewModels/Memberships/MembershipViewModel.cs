using System.Collections.Generic;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Hub.ViewModels.Memberships
{
    public class MembershipViewModel : MembershipViewModelBase, IHasIdentifierViewModel, IHasSysViewModel
    {
        #region Properties

        public string Id { get; set; }

        public SysModel Sys { get; set; }
        
        public MembershipMailSettings MailSettings { get; set; }
        
        public IEnumerable<MailHook> MailHooks { get; set; }

        #endregion

        #region Methods

        public DeleteViewModel GetMailHookDeleteViewModel()
        {
            return new DeleteViewModel
            {
                ControllerName = "Memberships",
                DeleteActionName = "DeleteMailHook",
                IsLocalizedContent = false
            };
        }

        #endregion
    }
}