using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Identity;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class OneTimePasswordRepository : RepositoryBase<OneTimePassword>, IOneTimePasswordRepository
{
    #region Properties
    
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("user_id"),
        new CompoundIndexDefinition("email_address", "password", "membership_id")
    };
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public OneTimePasswordRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<OneTimePasswordRepository> logger) : 
        base(clientProvider, settings, logger, "otps")
    {
        
    }
    
    #endregion
}