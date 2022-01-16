using System.ComponentModel.DataAnnotations;

namespace ErtisAuth.Hub.ViewModels.Auth
{
    public class ResetPasswordViewModel : ViewModelBase
    {
        #region Properties

        public string EmailAddress { get; set; }
		
        [Required(ErrorMessage = "Please enter a new password")]
        [StringLength(int.MaxValue, MinimumLength = 6, ErrorMessage = "Password is at least 6 characters")]
        public string NewPassword { get; set; }
		
        [Required(ErrorMessage = "Please re-enter new password")]
        [StringLength(int.MaxValue, MinimumLength = 6, ErrorMessage = "Password is at least 6 characters")]
        [CompareAttribute("NewPassword", ErrorMessage = "Passwords does not match")]
        public string NewPasswordAgain { get; set; }
		
        public string ResetPasswordToken { get; set; }

        #endregion
    }
}