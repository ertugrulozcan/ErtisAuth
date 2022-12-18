using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using ErtisAuth.Integrations.OAuth.Microsoft;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Integrations.OAuth.Extensions
{
	public static class DependencyInjectionExtensions
	{
		#region Methods

		public static void AddProviders(this IServiceCollection services)
		{
			services.AddSingleton<IFacebookAuthenticator, FacebookAuthenticator>();
			services.AddSingleton<IGoogleAuthenticator, GoogleAuthenticator>();
			services.AddSingleton<IMicrosoftAuthenticator, MicrosoftAuthenticator>();
		}
		
		public static void UseProviders(this IApplicationBuilder applicationBuilder)
		{
			AuthenticatorFactory.Current.Configure(applicationBuilder.ApplicationServices);
		}

		#endregion
	}
}