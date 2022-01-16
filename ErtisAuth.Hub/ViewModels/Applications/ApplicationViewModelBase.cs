using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ErtisAuth.Hub.ViewModels.Applications
{
    public abstract class ApplicationViewModelBase : ViewModelBase
    {
        #region Properties

        [Required]
        public string Name { get; set; }
		
        [Required]
        public string Role { get; set; }
        
        public List<SelectListItem> RoleList { get; set; }
        
        #endregion
    }
}