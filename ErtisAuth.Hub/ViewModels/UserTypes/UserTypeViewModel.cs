using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Hub.ViewModels.UserTypes
{
    public class UserTypeViewModel : ViewModelBase
    {
        #region Properties

        public Membership Membership { get; set; }

        public UserType UserType => this.Membership?.UserType;

        #endregion
    }
}