using System.Collections.Generic;
using System.Threading.Tasks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;
using GoogleOAuth = Google.Apis.Auth;

namespace ErtisAuth.Integrations.OAuth.Google
{
	public interface IGoogleAuthenticator : IProviderAuthenticator, IProviderAuthenticator<GoogleLoginRequest, GoogleToken, GoogleUser>
	{}
	
	public class GoogleAuthenticator : IGoogleAuthenticator
	{
		#region Methods

		public async Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider)
		{
			return await this.VerifyTokenAsync(request as GoogleLoginRequest, provider);
		}
		
		public async Task<bool> VerifyTokenAsync(GoogleLoginRequest request, Provider provider)
		{
			if (provider.AppClientId == request.ClientId)
			{
				var googleUser = await GoogleOAuth.GoogleJsonWebSignature.ValidateAsync(
					request.AccessToken, // ID Token
					new GoogleOAuth.GoogleJsonWebSignature.ValidationSettings
					{
						Audience = new List<string> { provider.AppClientId }
					});

				request.Token.ExpiresIn = googleUser.ExpirationTimeSeconds ?? 0;
				
				request.User = new GoogleUser
				{
					Id = googleUser.Subject,
					FirstName = googleUser.GivenName,
					LastName = googleUser.FamilyName,
					EmailAddress = googleUser.Email,
					Scope = googleUser.Scope,
					Prn = googleUser.Prn,
					HostedDomain = googleUser.HostedDomain,
					EmailVerified = false,
					FullName = googleUser.Name,
					Picture = googleUser.Picture,
					Locale = googleUser.Locale
				};
				
				return request.IsValid();
			}
			else
			{
				throw ErtisAuthException.UntrustedProvider();
			}
		}

		public async Task<bool> RevokeTokenAsync(string accessToken, Provider provider)
		{
			// Google ID tokens not revoke, they already have a short lifetime.
			await Task.CompletedTask;
			return true;
		}
        
		#endregion
	}
}