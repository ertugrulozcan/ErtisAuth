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
		return provider.Type switch
		{
			ProviderType.Facebook => this._serviceProvider.GetRequiredService<IFacebookAuthenticator>(),
			ProviderType.Google => this._serviceProvider.GetRequiredService<IGoogleAuthenticator>(),
			ProviderType.Microsoft => this._serviceProvider.GetRequiredService<IMicrosoftAuthenticator>(),
			ProviderType.Apple or ProviderType.AppleNative => this._serviceProvider.GetRequiredService<IAppleAuthenticator>(),
			_ => throw ErtisAuthException.UnsupportedProvider()
		};
	}
	
	#endregion
}