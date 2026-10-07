using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Events;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IEventRepository : IMongoRepository<ErtisAuthEvent>;