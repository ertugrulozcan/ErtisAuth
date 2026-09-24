using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IOneTimePasswordRepository : IMongoRepository<OneTimePassword>;