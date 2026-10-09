using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using GoogleOAuth = Google.Apis.Auth;

namespace ErtisAuth.Integrations.OAuth.Google;

public interface IGoogleAuthenticator : IProviderAuthenticator<GoogleProvider, GoogleLoginRequest, GoogleToken, GoogleUser>;

public class GoogleAuthenticator : IGoogleAuthenticator
{
	#region Services
	
	private readonly IGoogleIdTokenValidator _idTokenValidator;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="idTokenValidator"></param>
	public GoogleAuthenticator(IGoogleIdTokenValidator idTokenValidator)
	{
		this._idTokenValidator = idTokenValidator;
	}
	
	#endregion
	
	#region Methods
	
	public async Task<bool> VerifyTokenAsync(GoogleLoginRequest request, GoogleProvider provider, CancellationToken cancellationToken = default)
	{
		if (request.Token == null || string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(provider.AppClientId))
		{
			return false;
		}
		
		if (provider.AppClientId == request.ClientId)
		{
			GoogleOAuth.GoogleJsonWebSignature.Payload googleUser;
			try
			{
				// AccessToken is the ID token
				googleUser = await this._idTokenValidator.ValidateAsync(request.AccessToken, provider.AppClientId);
			}
			catch (GoogleOAuth.InvalidJwtException)
			{
				return false;
			}
			
			request.Token.ExpiresIn = googleUser.ExpirationTimeSeconds ?? 0;
			
			request.User = new GoogleUser
			{
				Id = googleUser.Subject,
				// Names written in a single field may come only as "name"
				FirstName = string.IsNullOrEmpty(googleUser.GivenName) ? googleUser.Name : googleUser.GivenName,
				LastName = googleUser.FamilyName,
				EmailAddress = googleUser.Email,
				Scope = googleUser.Scope,
				Prn = googleUser.Prn,
				HostedDomain = googleUser.HostedDomain,
				EmailVerified = googleUser.EmailVerified,
				FullName = googleUser.Name,
				Picture = googleUser.Picture,
				Locale = googleUser.Locale
			};
			
			// A valid token without these claims means the client did not request the "email" / "profile" scopes
			if (string.IsNullOrEmpty(request.User.EmailAddress) || string.IsNullOrEmpty(request.User.FirstName))
			{
				throw ErtisAuthException.ProviderProfileIncomplete(provider.Name, "email and name are required, request the 'email' and 'profile' scopes");
			}
			
			return request.IsValid();
		}
		else
		{
			throw ErtisAuthException.UntrustedProvider();
		}
	}
	
	public async Task<bool> RevokeTokenAsync(string accessToken, GoogleProvider provider, CancellationToken cancellationToken = default)
	{
		// Google ID tokens not revoke, they already have a short lifetime.
		return await Task.FromResult(true);
	}
    
	#endregion
}