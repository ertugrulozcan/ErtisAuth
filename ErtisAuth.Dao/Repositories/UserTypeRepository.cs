using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Users;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class UserTypeRepository : RepositoryBase<UserType>, IUserTypeRepository
{
    #region Properties
    
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("slug"),
        new SingleIndexDefinition("membership_id"),
        new CompoundIndexDefinition("_id", "membership_id"),
        new CompoundIndexDefinition("slug", "membership_id")
    };
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public UserTypeRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<UserTypeRepository> logger) : 
        base(clientProvider, settings, logger, "user-types")
    {
		
    }
    
    #endregion
}