using Ertis.Data.Repository;
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
        /// <param name="settings"></param>
        /// <param name="actionBinder"></param>
        public UserTypeRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "user-types", actionBinder)
        {
			
        }

        #endregion
    }
}