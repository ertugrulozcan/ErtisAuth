using System.ComponentModel.DataAnnotations;

namespace ErtisAuth.Hub.ViewModels.Users
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        #region Properties

        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }
		
        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
		
        [Required]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }

        #endregion
    }
}