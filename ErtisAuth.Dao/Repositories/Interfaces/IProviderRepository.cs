using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Providers;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IProviderRepository : IMongoRepository<Provider>;