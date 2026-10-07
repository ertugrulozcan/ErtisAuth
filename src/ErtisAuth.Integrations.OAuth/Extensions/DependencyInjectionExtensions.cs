using ErtisAuth.Integrations.OAuth.Apple;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using ErtisAuth.Integrations.OAuth.Microsoft;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Integrations.OAuth.Extensions;

public static class DependencyInjectionExtensions
{
	#region Methods
	
	public static void AddProviders(this IServiceCollection services)
	{
		services.AddSingleton<IFacebookAuthenticator, FacebookAuthenticator>();
		services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
		services.AddSingleton<IGoogleAuthenticator, GoogleAuthenticator>();
		services.AddSingleton<IMicrosoftAuthenticator, MicrosoftAuthenticator>();
		services.AddSingleton<IAppleAuthenticator, AppleAuthenticator>();
		services.AddSingleton<IAuthenticatorFactory, AuthenticatorFactory>();
	}
	
	#endregion
}