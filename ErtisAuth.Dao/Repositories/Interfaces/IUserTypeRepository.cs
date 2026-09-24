using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IUserTypeRepository : IMongoRepository<UserType>;