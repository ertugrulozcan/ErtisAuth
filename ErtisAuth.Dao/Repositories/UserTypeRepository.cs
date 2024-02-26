using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Dao.Repositories
{
    public class UserTypeRepository : RepositoryBase<UserTypeDto>, IUserTypeRepository
    {
        #region Properties
        
        protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
        {
            new SingleIndexDefinition("slug"),
            new SingleIndexDefinition("membership_id"),
            new CompoundIndexDefinition("_id", "membership_id"),
            new CompoundIndexDefinition("slug", "membership_id"),
        };

        #endregion
        
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