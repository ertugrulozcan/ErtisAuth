using ErtisAuth.Core.Models.Providers;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IProviderService : IMembershipBoundedCrudService<OAuthProvider>
	{
		
	}
}