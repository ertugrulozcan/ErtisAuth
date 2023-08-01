using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;
using MongoDB.Driver.Core.Events;

namespace ErtisAuth.Dao.Repositories
{
    public class UserTypeRepository : RepositoryBase<UserTypeDto>, IUserTypeRepository
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="clientSettings"></param>
        /// <param name="actionBinder"></param>
        /// <param name="eventSubscriber"></param>
        public UserTypeRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder, IEventSubscriber eventSubscriber) : 
            base(settings, "user-types", clientSettings, actionBinder, eventSubscriber)
        {
			
        }

        #endregion
    }
}