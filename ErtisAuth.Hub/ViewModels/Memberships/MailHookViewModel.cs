using Ertis.Core.Models.Resources;

namespace ErtisAuth.Hub.ViewModels.Memberships
{
    public class MailHookViewModel : MailHookViewModelBase, IHasIdentifierViewModel, IHasSysViewModel
    {
        #region Properties

        public string Id { get; set; }

        public SysModel Sys { get; set; }
        
        #endregion
    }
}