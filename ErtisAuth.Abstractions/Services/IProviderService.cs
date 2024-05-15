using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Abstractions.Services
{
	public interface IProviderService : IMembershipBoundedCrudService<Provider>
	{
		Task<IEnumerable<Provider>> GetProvidersAsync(string membershipId, CancellationToken cancellationToken = default);
		
		ValueTask<BearerToken> LoginAsync(IProviderLoginRequest request, string membershipId, string ipAddress = null, string userAgent = null, CancellationToken cancellationToken = default);
		
		Task LogoutAsync(string token, CancellationToken cancellationToken = default);
	}
}