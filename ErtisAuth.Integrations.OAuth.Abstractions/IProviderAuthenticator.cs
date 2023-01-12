using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Integrations.OAuth.Abstractions
{
	public interface IProviderAuthenticator
	{
		Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider, CancellationToken cancellationToken = default);
		
		Task<bool> RevokeTokenAsync(string accessToken, Provider provider, CancellationToken cancellationToken = default);
	}
	
	public interface IProviderAuthenticator<in TProviderLoginRequest, TToken, TUser> where TProviderLoginRequest : IProviderLoginRequest<TToken, TUser> where TToken : IProviderToken where TUser : IProviderUser
	{
		Task<bool> VerifyTokenAsync(TProviderLoginRequest request, Provider provider, CancellationToken cancellationToken = default);
	}
}