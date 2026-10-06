using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IProviderRepository : IMongoRepository<Provider>
{
	/// <summary>
	/// The membership's provider of the given type, by the "type" discriminator (Provider.Type is not a mapped member).
	/// </summary>
	Task<Provider?> FindOneByTypeAsync(ProviderType type, string membershipId, CancellationToken cancellationToken = default);
}