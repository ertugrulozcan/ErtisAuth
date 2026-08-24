// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Helpers;

public static class ReservedRoles
{
    #region Constants
    
    public const string Administrator = "admin";
    public const string Server = "server";
    
    #endregion
    
    #region Methods
    
    public static string[] ToArray()
    {
        return new[]
        {
            Administrator,
            Server
        };
    }
    
    #endregion
}