using System;

namespace ErtisAuth.Core.Constants;

public static class TTLs
{
    #region Constants

    public static readonly TimeSpan ACTIVATION_TOKEN_TTL = TimeSpan.FromHours(72);
    public static readonly TimeSpan RESET_PASSWORD_TOKEN_TTL = TimeSpan.FromHours(2);

    #endregion
}