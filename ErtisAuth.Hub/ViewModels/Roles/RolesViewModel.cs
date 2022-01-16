namespace ErtisAuth.Hub.ViewModels.Roles
{
    public class RolesViewModel : ViewModelBase, IHasDeleteViewModel, IHasBulkDeleteViewModel
    {
        #region Properties

        public RoleCreateViewModel CreateViewModel { get; set; }

        #endregion

        #region Methods

        public DeleteViewModel GetDeleteViewModel()
        {
            return new DeleteViewModel
            {
                ControllerName = "Roles",
                DeleteActionName = "Delete",
                IsLocalizedContent = false
            };
        }
        
        public BulkDeleteViewModel GetBulkDeleteViewModel()
        {
            return new BulkDeleteViewModel
            {
                ControllerName = "Roles",
                RbacResourceSlug = "roles",
                DeleteActionName = "BulkDelete",
                IsLocalizedContent = false
            };
        }

        #endregion
    }
}