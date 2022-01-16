namespace ErtisAuth.Hub.ViewModels.Users
{
    public class UsersViewModel : ViewModelBase, IHasDeleteViewModel, IHasBulkDeleteViewModel
    {
        #region Properties
        
        public UserCreateViewModel CreateViewModel { get; set; }
		
        #endregion

        #region Methods

        public DeleteViewModel GetDeleteViewModel()
        {
            return new DeleteViewModel
            {
                ControllerName = "Users",
                DeleteActionName = "Delete",
                IsLocalizedContent = false
            };
        }
        
        public BulkDeleteViewModel GetBulkDeleteViewModel()
        {
            return new BulkDeleteViewModel
            {
                ControllerName = "Users",
                RbacResourceSlug = "users",
                DeleteActionName = "BulkDelete",
                IsLocalizedContent = false
            };
        }

        #endregion
    }
}