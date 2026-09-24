using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

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
    
    #region Constructor
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public UserRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<UserRepository> logger) : 
        base(clientProvider, settings, logger, "users")
    {
        
    }
    
    #endregion
}