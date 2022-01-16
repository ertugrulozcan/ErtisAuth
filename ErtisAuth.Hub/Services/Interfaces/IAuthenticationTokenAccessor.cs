using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Hub.Services.Interfaces
{
    public interface IAuthenticationTokenAccessor
    {
        #region Properties

        TokenBase Token { get; set; }

        #endregion
    }
}