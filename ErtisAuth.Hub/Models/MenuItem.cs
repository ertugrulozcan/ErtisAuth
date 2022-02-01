using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Hub.Helpers;

namespace ErtisAuth.Hub.Models
{
    public class MenuItem
    {
        #region Properties

        public string Title { get; set; }
        
        public string Icon { get; set; }

        public string IconSvg => SvgIcons.GetByName(this.Icon);

        public bool IsActive { get; set; }
        
        public bool IsDisabled { get; set; }
        
        public string Controller { get; set; }

        public bool HasController => !string.IsNullOrEmpty(this.Controller);
        
        public string Action { get; set; }
        
        public bool HasAction => !string.IsNullOrEmpty(this.Action);
        
        public string RawUrl { get; set; }
        
        public bool HasRawUrl => !string.IsNullOrEmpty(this.RawUrl);
        
        public IEnumerable<MenuItem> SubMenuItems { get; set; }

        public bool HasSubMenu => this.SubMenuItems != null && this.SubMenuItems.Any();

        #endregion
    }
}