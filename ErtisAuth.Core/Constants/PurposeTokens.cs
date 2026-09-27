namespace ErtisAuth.Core.Constants;

/// <summary>
/// Single-purpose tokens (password reset, user activation) are JWTs signed with the membership key like access tokens,
/// so they carry a token type claim and are never accepted where another type of token is expected.
/// </summary>
public static class PurposeTokens
{
	#region Constants
	
	public const string TokenTypeClaim = "token_type";
	
	public const string ResetPasswordTokenType = "reset_token";
	
	public const string ActivationTokenType = "activation_token";
	
	/// <summary>
	/// Fingerprint of the password hash a reset token was issued for; the token becomes invalid once the password changes.
	/// </summary>
	public const string PasswordFingerprintClaim = "pwd";
	
	#endregion
}
