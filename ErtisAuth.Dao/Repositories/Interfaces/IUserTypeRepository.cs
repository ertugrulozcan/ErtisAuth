using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
    public interface IUserTypeRepository : IMongoRepository<UserTypeDto>
    {
        
    }
}