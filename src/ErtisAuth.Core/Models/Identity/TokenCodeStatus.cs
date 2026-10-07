namespace ErtisAuth.Core.Models.Identity;

/// <summary>
/// The states of a token code.
/// </summary>
public static class TokenCodeStatus
{
	#region Constants
	
	public const string Pending = "pending";
	
	public const string Approved = "approved";
	
	public const string Denied = "denied";
	
	#endregion
}
