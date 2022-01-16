using Ertis.Core.Models.Resources;

namespace ErtisAuth.Hub.ViewModels.Webhooks
{
    public class WebhookViewModel : WebhookViewModelBase, IHasIdentifierViewModel, IHasSysViewModel
    {
        #region Properties

        public string Id { get; set; }

        public SysModel Sys { get; set; }
        
        #endregion
    }
}