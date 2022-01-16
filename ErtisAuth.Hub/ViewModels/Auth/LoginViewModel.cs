using System.ComponentModel.DataAnnotations;

namespace ErtisAuth.Hub.ViewModels.Auth
{
    public class LoginViewModel : ViewModelBase
    {
        #region Properties

        [Required]
        public string Username { get; set; }
		
        [Required]
        public string Password { get; set; }
		
        public string ReturnUrl { get; set; }

        public string ClientIP { get; set; }
		
        public string UserAgentRaw { get; set; }
		
        #endregion
    }
}