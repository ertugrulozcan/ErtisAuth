using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Identity;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class CodePolicyRepository : RepositoryBase<TokenCodePolicy>, ICodePolicyRepository
{
    #region Properties
    
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("slug"),
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
    public CodePolicyRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<CodePolicyRepository> logger) : 
        base(clientProvider, settings, logger, "code-policies")
    {
        
    }
    
    #endregion
}