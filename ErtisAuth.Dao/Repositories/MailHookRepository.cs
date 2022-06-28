using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;

namespace ErtisAuth.Dao.Repositories
{
    public class MailHookRepository : MongoRepositoryBase<MailHookDto>, IMailHookRepository
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="actionBinder"></param>
        public MailHookRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "mailhooks", actionBinder)
        {
			
        }

        #endregion
    }
}