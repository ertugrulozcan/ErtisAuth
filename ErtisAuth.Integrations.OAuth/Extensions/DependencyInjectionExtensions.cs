using ErtisAuth.Integrations.OAuth.Facebook;
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
		}
		
		public static void UseProviders(this IApplicationBuilder applicationBuilder)
		{
			AuthenticatorFactory.Current.Configure(applicationBuilder.ApplicationServices);
		}

		#endregion
	}
}