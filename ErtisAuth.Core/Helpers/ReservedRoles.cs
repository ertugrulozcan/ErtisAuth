// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Helpers;

public static class ReservedRoles
{
    #region Constants
    
    public const string Administrator = "admin";
    
    #endregion
    
    #region Methods
    
    public static string[] ToArray()
    {
        return new[]
        {
            Administrator
        };
    }
    
    #endregion
}