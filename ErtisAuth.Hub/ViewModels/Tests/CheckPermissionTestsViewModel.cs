using System;
using System.Collections.Generic;

namespace ErtisAuth.Hub.ViewModels.Tests
{
    public class CheckPermissionTestsViewModel : ViewModelBase
    {
        #region Properties
        
        public IEnumerable<CheckPermissionTestResult> Results { get; set; }
        
        public TimeSpan TotalTime { get; set; }

        #endregion
    }
}