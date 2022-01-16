using System.ComponentModel.DataAnnotations;

namespace ErtisAuth.Hub.ViewModels
{
    public class DeleteViewModel
    {
        #region Properties

        [Required]
        public string ItemId { get; set; } = string.Empty;
		
        public string ItemContentId { get; set; } = string.Empty;
		
        public bool DeleteAll { get; set; }
		
        [Required(ErrorMessage = "The password cannot be empty")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        public string ControllerName { get; init; }

        public string DeleteActionName { get; init; }
        
        public bool IsLocalizedContent { get; init; }

        #endregion
    }
}