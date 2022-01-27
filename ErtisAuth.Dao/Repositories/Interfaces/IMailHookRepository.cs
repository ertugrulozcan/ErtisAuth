using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Mailing;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
    public interface IMailHookRepository : IMongoRepository<MailHookDto>
    {
        
    }
}