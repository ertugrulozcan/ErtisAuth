using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IRoleRepository : IMongoRepository<Role>;