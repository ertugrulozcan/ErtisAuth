using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Integrations.OAuth.Abstractions;

public interface IProviderAuthenticator
{
	Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider, CancellationToken cancellationToken = default);
	
	Task<bool> RevokeTokenAsync(string accessToken, Provider provider, CancellationToken cancellationToken = default);
}

/*
public interface IProviderAuthenticator<in TProvider> : IProviderAuthenticator where TProvider : Provider
{
	Task<bool> VerifyTokenAsync(IProviderLoginRequest request, TProvider provider, CancellationToken cancellationToken = default);
	
	Task<bool> RevokeTokenAsync(string accessToken, TProvider provider, CancellationToken cancellationToken = default);
}
*/

public interface IProviderAuthenticator<in TProvider, in TProviderLoginRequest, TToken, TUser> : IProviderAuthenticator 
	where TProvider : Provider 
	where TProviderLoginRequest : IProviderLoginRequest<TToken, TUser> 
	where TToken : IProviderToken 
	where TUser : IProviderUser
{
	Task<bool> VerifyTokenAsync(TProviderLoginRequest request, TProvider provider, CancellationToken cancellationToken = default);
	
	Task<bool> RevokeTokenAsync(string accessToken, TProvider provider, CancellationToken cancellationToken = default);
	
	Task<bool> IProviderAuthenticator.VerifyTokenAsync(IProviderLoginRequest request, Provider provider, CancellationToken cancellationToken)
	{
		if (request is not TProviderLoginRequest typedRequest)
		{
			throw new ArgumentException($"The {this.GetType().Name} can not use with a {request.GetType().Name} (expected {typeof(TProviderLoginRequest).Name})", nameof(request));
		}
		
		if (provider is not TProvider typedProvider)
		{
			throw new ArgumentException($"The {this.GetType().Name} can not use with a {provider.GetType().Name} (expected {typeof(TProvider).Name})", nameof(provider));
		}
		
		return this.VerifyTokenAsync(typedRequest, typedProvider, cancellationToken);
	}
	
	Task<bool> IProviderAuthenticator.RevokeTokenAsync(string accessToken, Provider provider, CancellationToken cancellationToken)
	{
		if (provider is not TProvider typedProvider)
		{
			throw new ArgumentException($"The {this.GetType().Name} can not use with a {provider.GetType().Name} (expected {typeof(TProvider).Name})", nameof(provider));
		}
		
		return this.RevokeTokenAsync(accessToken, typedProvider, cancellationToken);
	}
}