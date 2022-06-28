using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;

namespace ErtisAuth.Dao.Repositories
{
    public class UserRepository : DynamicMongoRepository, IUserRepository
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="actionBinder"></param>
        public UserRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "users", actionBinder)
        {
            
        }

        #endregion
    }
}