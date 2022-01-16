using System.ComponentModel.DataAnnotations;

namespace ErtisAuth.Hub.ViewModels
{
    public class BulkDeleteViewModel
    {
        #region Properties

        public bool DeleteAll { get; set; }
		
        [Required(ErrorMessage = "The password cannot be empty")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        public string ControllerName { get; init; }

        public string DeleteActionName { get; init; }
        
        public bool IsLocalizedContent { get; init; }

        public string RbacResourceSlug { get; init; }
        
        public string ItemIdsJson { get; init; }

        #endregion
    }
}