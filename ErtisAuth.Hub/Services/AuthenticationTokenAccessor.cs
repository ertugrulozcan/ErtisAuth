using ErtisAuth.Hub.Services.Interfaces;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Hub.Services
{
    public class AuthenticationTokenAccessor : IAuthenticationTokenAccessor
    {
        #region Properties

        public TokenBase Token { get; set; }

        #endregion
    }
}