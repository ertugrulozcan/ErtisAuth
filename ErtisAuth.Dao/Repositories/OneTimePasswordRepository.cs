using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories;

public class OneTimePasswordRepository : RepositoryBase<OneTimePasswordDto>, IOneTimePasswordRepository
{
    #region Properties
        
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("user_id"),
        new CompoundIndexDefinition("email_address", "password", "membership_id"),
    };

    #endregion
		
    #region Constructors

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    public OneTimePasswordRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings) : 
        base(clientProvider, settings, "otps")
    {
			
    }

    #endregion
}