using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Core.Helpers;

public static class ReservedRoles
{
    #region Constants
    
    public static readonly Role Administrator = new()
    {
        Name = "Administrator",
        Slug = "admin",
        Description = "Administrator",
        MembershipId = null!
    };
    
    #endregion
    
    #region Methods
    
    private static Role[] GetList()
    {
        return new[]
        {
            Administrator
        };
    }
    
    public static bool IsReserved(string slug)
    {
        var roles = GetList();
        return roles.Any(x => x.Slug == slug);
    }
    
    #endregion
}