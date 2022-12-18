using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;

namespace ErtisAuth.Integrations.OAuth.Extensions
{
	public static class ProviderExtensions
	{
		#region Methods

		public static IProviderAuthenticator GetAuthenticator(this Provider provider)
		{
			return AuthenticatorFactory.Current.GetAuthenticator(provider);
		}

		#endregion
	}
}