using System;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Integrations.OAuth
{
	public class AuthenticatorFactory
	{
		#region Services

		private IServiceProvider _serviceProvider;

		#endregion
		
		#region Fields

		private static AuthenticatorFactory self;

		#endregion

		#region Properties

		public static AuthenticatorFactory Current
		{
			get
			{
				if (self == null)
				{
					self = new AuthenticatorFactory();
				}

				return self;
			}
		}

		#endregion

		#region Methods

		internal void Configure(IServiceProvider serviceProvider)
		{
			this._serviceProvider = serviceProvider;
		}

		public IProviderAuthenticator GetAuthenticator(Provider provider)
		{
			if (this._serviceProvider == null)
			{
				throw new Exception("AuthenticatorFactory was not configured yet");
			}

			if (provider.Name == KnownProviders.Facebook.ToString())
			{
				return this._serviceProvider.GetRequiredService<IFacebookAuthenticator>();
			}
			else if (provider.Name == KnownProviders.Google.ToString())
			{
				return this._serviceProvider.GetRequiredService<IGoogleAuthenticator>();
			}
			else
			{
				throw ErtisAuthException.UnsupportedProvider();
			}
		}

		#endregion
	}
}