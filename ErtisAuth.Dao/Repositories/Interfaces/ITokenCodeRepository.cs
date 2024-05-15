using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface ITokenCodeRepository : IMongoRepository<TokenCodeDto>
{
    
}