using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;
using MongoDB.Driver.Core.Events;

namespace ErtisAuth.Dao.Repositories
{
    public class MailHookRepository : RepositoryBase<MailHookDto>, IMailHookRepository
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="clientSettings"></param>
        /// <param name="actionBinder"></param>
        /// <param name="eventSubscriber"></param>
        public MailHookRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder, IEventSubscriber eventSubscriber) : 
            base(settings, "mailhooks", clientSettings, actionBinder, eventSubscriber)
        {
			
        }

        #endregion
    }
}