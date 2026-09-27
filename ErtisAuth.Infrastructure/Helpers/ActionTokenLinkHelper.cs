using System.Text;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// Reset password and activation links carry base64("membershipId:token") in a query parameter of the host url.
/// </summary>
public static class ActionTokenLinkHelper
{
	#region Constants
	
	public const string ResetPasswordQueryParameter = "rpt";
	
	public const string ActivationQueryParameter = "uat";
	
	#endregion
	
	#region Methods
	
	public static string Encode(string membershipId, string token)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{membershipId}:{token}"));
	}
	
	public static string GenerateLink(string host, string queryParameter, string membershipId, string token)
	{
		return $"{host.TrimEnd('/')}?{queryParameter}={Encode(membershipId, token)}";
	}
	
	/// <summary>
	/// Returns the token in the given code when it belongs to the membership, otherwise null.
	/// </summary>
	public static string? Decode(string membershipId, string code)
	{
		string payload;
		try
		{
			try
			{
				payload = Encoding.UTF8.GetString(Convert.FromBase64String(code));
			}
			catch (FormatException)
			{
				payload = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(code);
			}
		}
		catch (Exception)
		{
			return null;
		}
		
		var separatorIndex = payload.IndexOf(':');
		if (separatorIndex <= 0 || payload[..separatorIndex] != membershipId)
		{
			return null;
		}
		
		var token = payload[(separatorIndex + 1)..];
		return string.IsNullOrEmpty(token) ? null : token;
	}
	
	#endregion
}
