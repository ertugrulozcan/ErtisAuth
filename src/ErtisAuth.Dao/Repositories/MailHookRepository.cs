using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Mailing;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class MailHookRepository : RepositoryBase<MailHook>, IMailHookRepository
{
    #region Properties
    
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("slug"),
        new SingleIndexDefinition("event"),
        new SingleIndexDefinition("membership_id")
    };
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public MailHookRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<MailHookRepository> logger) : 
        base(clientProvider, settings, logger, "mailhooks")
    {
		
    }
    
    #endregion
}