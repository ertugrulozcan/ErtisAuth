using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories;

public class TokenCodeRepository : RepositoryBase<TokenCodeDto>, ITokenCodeRepository
{
    #region Properties
        
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("user_id"),
        new SingleIndexDefinition("code"),
        new SingleIndexDefinition("membership_id"),
        new CompoundIndexDefinition("user_id", "membership_id"),
        new CompoundIndexDefinition("code", "membership_id"),
    };

    #endregion
		
    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    public TokenCodeRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings) : 
        base(clientProvider, settings, "codes")
    {
			
    }

    #endregion
}