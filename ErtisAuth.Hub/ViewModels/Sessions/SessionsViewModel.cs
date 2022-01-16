using System.Collections.Generic;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Hub.ViewModels.Sessions
{
    public class SessionsViewModel : ViewModelBase
    {
        #region Properties

        public Dictionary<string, List<object>> GroupedActiveTokensByCity { get; set; }
        
        public Dictionary<string, List<object>> GroupedActiveTokensByCountry { get; set; }

        #endregion
    }
}