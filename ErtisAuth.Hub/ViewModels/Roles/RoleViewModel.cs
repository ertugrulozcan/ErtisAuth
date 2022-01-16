using Ertis.Core.Models.Resources;

namespace ErtisAuth.Hub.ViewModels.Roles
{
    public class RoleViewModel : RoleViewModelBase, IHasIdentifierViewModel, IHasSysViewModel
    {
        #region Properties

        public string Id { get; set; }

        public SysModel Sys { get; set; }

        #endregion
    }
}