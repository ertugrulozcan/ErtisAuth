using GoogleOAuth = Google.Apis.Auth;

namespace ErtisAuth.Integrations.OAuth.Google;

/// <summary>
/// Validates a Google ID token (signature against Google's certificates, issuer, audience, expiration).
/// A seam around GoogleJsonWebSignature, which downloads the certificates with its own HTTP client.
/// </summary>
public interface IGoogleIdTokenValidator
{
	/// <exception cref="GoogleOAuth.InvalidJwtException">The token is not a valid Google ID token for the audience.</exception>
	Task<GoogleOAuth.GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string audience);
}

public class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
	#region Methods
	
	public async Task<GoogleOAuth.GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string audience)
	{
		return await GoogleOAuth.GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleOAuth.GoogleJsonWebSignature.ValidationSettings
		{
			Audience = new List<string> { audience }
		});
	}
	
	#endregion
}