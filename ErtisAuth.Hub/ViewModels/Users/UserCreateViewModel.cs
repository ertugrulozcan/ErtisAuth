using System.ComponentModel.DataAnnotations;

namespace ErtisAuth.Hub.ViewModels.Users
{
    public class UserCreateViewModel : UserViewModelBase
    {
        #region Properties

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
		
        [Required]
        [DataType(DataType.Password)]
        public string PasswordAgain { get; set; }

        #endregion
    }
}