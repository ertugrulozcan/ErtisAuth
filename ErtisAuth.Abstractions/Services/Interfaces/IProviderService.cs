using System.Collections.Generic;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IProviderService : IMembershipBoundedCrudService<Provider>
	{
		Task<IEnumerable<Provider>> GetProvidersAsync(string membershipId);
		
		ValueTask<BearerToken> LoginAsync(IProviderLoginRequest request, string membershipId, string ipAddress = null, string userAgent = null);
		
		Task LogoutAsync(string token);
	}
}