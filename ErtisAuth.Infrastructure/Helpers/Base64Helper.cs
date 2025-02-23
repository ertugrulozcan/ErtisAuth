using System;
using System.Text;

namespace ErtisAuth.Infrastructure.Helpers;

public static class Base64Helper
{
    #region Methods

    public static string Decode(string base64, Encoding encoding)
    {
        try
        {
            return encoding.GetString(Convert.FromBase64String(base64));
        }
        catch (FormatException)
        {
            return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(base64);
        }
    }

    #endregion
}