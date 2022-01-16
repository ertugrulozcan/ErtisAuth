using System.Collections.Generic;

namespace ErtisAuth.Hub.Models
{
    public class UbacTable
    {
        #region Properties

        public IEnumerable<UbacRow> Rows { get; init; }
        
        public IEnumerable<ExtendedUbac> MergedPermissions { get; init; }
        
        public IEnumerable<ExtendedUbac> MergedForbiddens { get; init; }

        #endregion
    }
}