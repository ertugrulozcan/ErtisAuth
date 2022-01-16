using Ertis.Core.Models.Resources;

namespace ErtisAuth.Hub.ViewModels.Applications
{
    public class ApplicationViewModel : ApplicationViewModelBase, IHasIdentifierViewModel, IHasSysViewModel
    {
        #region Properties

        public string Id { get; set; }

        public SysModel Sys { get; set; }

        public string BasicToken { get; set; }
        
        #endregion
    }
}