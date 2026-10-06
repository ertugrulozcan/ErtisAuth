using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Abstractions.Services;

public interface IProviderService : IMembershipBoundedCrudService<Provider>
{
	Task<Provider?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
	
	Task<IEnumerable<Provider>> GetProvidersAsync(string membershipId, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Signs in (or signs up) the user of the login request with the given provider of the membership (the provider's
	/// type must be the request's provider type).
	/// </summary>
	Task<BearerToken> LoginAsync(Provider provider, IProviderLoginRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
	
	Task LogoutAsync(string token, CancellationToken cancellationToken = default);
}