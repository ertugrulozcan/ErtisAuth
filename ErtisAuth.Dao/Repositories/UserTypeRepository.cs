using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Dao.Repositories
{
    public class UserTypeRepository : RepositoryBase<UserTypeDto>, IUserTypeRepository
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientProvider"></param>
        /// <param name="settings"></param>
        /// <param name="actionBinder"></param>
        public UserTypeRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
            base(clientProvider, settings, "user-types", actionBinder)
        {
			
        }

        #endregion
    }
}