using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Apple;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Facebook;
using ErtisAuth.Integrations.OAuth.Google;
using ErtisAuth.Integrations.OAuth.Microsoft;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Integrations.OAuth;

public interface IAuthenticatorFactory
{
	IProviderAuthenticator GetAuthenticator(Provider provider);
}

public class AuthenticatorFactory : IAuthenticatorFactory
{
	#region Services
	
	private readonly IServiceProvider _serviceProvider;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="serviceProvider"></param>
	public AuthenticatorFactory(IServiceProvider serviceProvider)
	{
		this._serviceProvider = serviceProvider;
	}
	
	#endregion
	
	#region Methods
	
	public IProviderAuthenticator GetAuthenticator(Provider provider)
	{
		if (provider.Name == KnownProviders.Facebook.ToString())
		{
			return this._serviceProvider.GetRequiredService<IFacebookAuthenticator>();
		}
		else if (provider.Name == KnownProviders.Google.ToString())
		{
			return this._serviceProvider.GetRequiredService<IGoogleAuthenticator>();
		}
		else if (provider.Name == KnownProviders.Microsoft.ToString())
		{
			return this._serviceProvider.GetRequiredService<IMicrosoftAuthenticator>();
		}
		else if (provider.Name == KnownProviders.Apple.ToString() || provider.Name == KnownProviders.AppleNative.ToString())
		{
			return this._serviceProvider.GetRequiredService<IAppleAuthenticator>();
		}
		else
		{
			throw ErtisAuthException.UnsupportedProvider();
		}
	}
	
	#endregion
}