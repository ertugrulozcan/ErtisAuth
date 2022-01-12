using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IProviderService : IReadonlyMembershipBoundedService<OAuthProvider>
	{
		
	}
}