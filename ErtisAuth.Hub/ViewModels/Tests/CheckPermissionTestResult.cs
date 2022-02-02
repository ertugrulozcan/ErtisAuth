using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Hub.ViewModels.Tests
{
    public class CheckPermissionTestResult
    {
        #region Properties

        public Rbac Rbac { get; set; }
        
        public bool? IsPermitted { get; set; }

        #endregion
    }
}