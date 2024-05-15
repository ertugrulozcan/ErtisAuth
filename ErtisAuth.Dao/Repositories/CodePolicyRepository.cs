using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories;

public class CodePolicyRepository : RepositoryBase<TokenCodePolicyDto>, ICodePolicyRepository
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
    public CodePolicyRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings) : 
        base(clientProvider, settings, "code-policies")
    {
			
    }

    #endregion
}