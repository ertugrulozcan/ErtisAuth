using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Applications;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IApplicationRepository : IMongoRepository<Application>;