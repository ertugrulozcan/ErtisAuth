namespace ErtisAuth.Hub.ViewModels.Memberships
{
    public class MembershipsViewModel : ViewModelBase, IHasDeleteViewModel, IHasBulkDeleteViewModel
    {
        #region Properties

        public MembershipCreateViewModel CreateViewModel { get; set; }

        #endregion

        #region Methods

        public DeleteViewModel GetDeleteViewModel()
        {
            return new DeleteViewModel
            {
                ControllerName = "Memberships",
                DeleteActionName = "Delete",
                IsLocalizedContent = false
            };
        }
        
        public BulkDeleteViewModel GetBulkDeleteViewModel()
        {
            return new BulkDeleteViewModel
            {
                ControllerName = "Memberships",
                RbacResourceSlug = "memberships",
                DeleteActionName = "BulkDelete",
                IsLocalizedContent = false
            };
        }

        #endregion
    }
}