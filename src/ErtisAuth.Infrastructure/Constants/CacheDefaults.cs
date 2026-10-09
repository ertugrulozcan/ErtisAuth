namespace ErtisAuth.Infrastructure.Constants;

public static class CacheDefaults
{
    #region Constants
    
    public static readonly TimeSpan MembershipsCacheTTL = TimeSpan.FromHours(1);
    
    public static readonly TimeSpan UserTypesCacheTTL = TimeSpan.FromHours(1);
    
    public static readonly TimeSpan RolesCacheTTL = TimeSpan.FromMinutes(5);
    
    public static readonly TimeSpan ApplicationsCacheTTL = TimeSpan.FromMinutes(5);
    
    public static readonly TimeSpan ProvidersCacheTTL = TimeSpan.FromHours(1);
    
    public static readonly TimeSpan RevokedTokensCacheTTL = TimeSpan.FromHours(24);
    
    #endregion
}