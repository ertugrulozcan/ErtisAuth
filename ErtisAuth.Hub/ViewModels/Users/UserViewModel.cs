using System.Collections.Generic;
using ErtisAuth.Hub.Models;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Hub.ViewModels.Users
{
    public class UserViewModel : UserViewModelBase, IHasDeleteViewModel, IHasIdentifierViewModel, IHasSysViewModel
    {
        #region Properties

        public string Id { get; set; }

        public IEnumerable<ActiveToken> ActiveTokens { get; set; }
        
        public IEnumerable<RevokedToken> RevokedTokens { get; set; }
        
        public UbacTable UbacTable { get; set; }

        public SysModel Sys { get; set; }

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

        #endregion
    }
}