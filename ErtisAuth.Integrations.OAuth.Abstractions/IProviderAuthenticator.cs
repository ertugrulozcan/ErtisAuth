using System.Threading.Tasks;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Integrations.OAuth.Abstractions
{
	public interface IProviderAuthenticator
	{
		Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider);
		
		Task<bool> RevokeTokenAsync(string accessToken, Provider provider);
	}
	
	public interface IProviderAuthenticator<in TProviderLoginRequest, TUser> where TProviderLoginRequest : IProviderLoginRequest<TUser> where TUser : IProviderUser
	{
		Task<bool> VerifyTokenAsync(TProviderLoginRequest request, Provider provider);
	}
}