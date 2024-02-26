using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;

namespace ErtisAuth.Dao.Repositories
{
    public class UserRepository : DynamicRepositoryBase, IUserRepository
    {
        #region Properties
        
        protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
        {
            new SingleIndexDefinition("username"),
            new SingleIndexDefinition("email_address"),
            new SingleIndexDefinition("membership_id"),
            new CompoundIndexDefinition("_id", "membership_id"),
            new CompoundIndexDefinition("username", "membership_id"),
            new CompoundIndexDefinition("email_address", "membership_id")
        };

        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientProvider"></param>
        /// <param name="settings"></param>
        /// <param name="actionBinder"></param>
        public UserRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
            base(clientProvider, settings, "users", actionBinder)
        {
            
        }

        #endregion
    }
}