namespace ErtisAuth.Hub.ViewModels.Webhooks
{
    public class WebhooksViewModel : ViewModelBase, IHasDeleteViewModel, IHasBulkDeleteViewModel
    {
        #region Properties

        public WebhookCreateViewModel CreateViewModel { get; set; }

        #endregion

        #region Methods

        public DeleteViewModel GetDeleteViewModel()
        {
            return new DeleteViewModel
            {
                ControllerName = "Webhooks",
                DeleteActionName = "Delete",
                IsLocalizedContent = false
            };
        }
        
        public BulkDeleteViewModel GetBulkDeleteViewModel()
        {
            return new BulkDeleteViewModel
            {
                ControllerName = "Webhooks",
                RbacResourceSlug = "webhooks",
                DeleteActionName = "BulkDelete",
                IsLocalizedContent = false
            };
        }

        #endregion
    }
}