using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Setup;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface ISetupTokenRepository : IMongoRepository<SetupToken>
{
	Task DropAsync(CancellationToken cancellationToken = default);
}