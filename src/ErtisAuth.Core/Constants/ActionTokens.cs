namespace ErtisAuth.Core.Constants;

/// <summary>
/// Action tokens authorize a single action (resetting the password, activating the user) instead of opening a session,
/// the same concept as Keycloak's action tokens. They are JWTs signed with the membership key like access tokens,
/// so they carry a token type claim and are never accepted where another type of token is expected.
/// </summary>
public static class ActionTokens
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
