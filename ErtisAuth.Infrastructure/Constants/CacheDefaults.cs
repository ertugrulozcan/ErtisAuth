using System;

namespace ErtisAuth.Infrastructure.Constants;

public static class CacheDefaults
{
    #region Properties

    public static readonly TimeSpan MembershipsCacheTTL = TimeSpan.FromMinutes(30);
    
    public static readonly TimeSpan UsersCacheTTL = TimeSpan.FromMinutes(5);
    
    public static readonly TimeSpan UserTypesCacheTTL = TimeSpan.FromMinutes(30);
    
    public static readonly TimeSpan RolesCacheTTL = TimeSpan.FromMinutes(10);
    
    public static readonly TimeSpan ApplicationsCacheTTL = TimeSpan.FromMinutes(30);
    
    public static readonly TimeSpan ProvidersCacheTTL = TimeSpan.FromMinutes(30);
    
    public static readonly TimeSpan RevokedTokensCacheTTL = TimeSpan.FromMinutes(3);

    #endregion
}