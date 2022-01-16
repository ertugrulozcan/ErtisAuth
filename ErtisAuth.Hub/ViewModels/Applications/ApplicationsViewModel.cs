namespace ErtisAuth.Hub.ViewModels.Applications
{
    public class ApplicationsViewModel : ViewModelBase, IHasDeleteViewModel, IHasBulkDeleteViewModel
    {
        #region Properties

        public ApplicationCreateViewModel CreateViewModel { get; set; }

        #endregion

        #region Methods

        public DeleteViewModel GetDeleteViewModel()
        {
            return new DeleteViewModel
            {
                ControllerName = "Applications",
                DeleteActionName = "Delete",
                IsLocalizedContent = false
            };
        }
        
        public BulkDeleteViewModel GetBulkDeleteViewModel()
        {
            return new BulkDeleteViewModel
            {
                ControllerName = "Applications",
                RbacResourceSlug = "applications",
                DeleteActionName = "BulkDelete",
                IsLocalizedContent = false
            };
        }

        #endregion
    }
}