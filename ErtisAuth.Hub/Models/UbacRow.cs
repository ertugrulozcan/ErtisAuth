using System.Collections.Generic;
using System.Linq;

namespace ErtisAuth.Hub.Models
{
    public class UbacRow
    {
        #region Properties

        public string ResourceName { get; init; }
        
        public UbacToggle CreateToggle { get; init; }
        
        public UbacToggle ReadToggle { get; init; }
        
        public UbacToggle UpdateToggle { get; init; }
        
        public UbacToggle DeleteToggle { get; init; }

        public IEnumerable<UbacToggle> Toggles =>
            Enumerable.Empty<UbacToggle>()
                .Append(this.CreateToggle)
                .Append(this.ReadToggle)
                .Append(this.UpdateToggle)
                .Append(this.DeleteToggle);

        #endregion
    }
}