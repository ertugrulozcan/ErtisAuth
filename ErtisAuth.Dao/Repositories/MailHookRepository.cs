using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Mailing;

namespace ErtisAuth.Dao.Repositories
{
    public class MailHookRepository : RepositoryBase<MailHookDto>, IMailHookRepository
    {
        #region Properties
        
        protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
        {
            new SingleIndexDefinition("slug"),
            new SingleIndexDefinition("event"),
            new SingleIndexDefinition("membership_id"),
        };

        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientProvider"></param>
        /// <param name="settings"></param>
        /// <param name="actionBinder"></param>
        public MailHookRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
            base(clientProvider, settings, "mailhooks", actionBinder)
        {
			
        }

        #endregion
    }
}