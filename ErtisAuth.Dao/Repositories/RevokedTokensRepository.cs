using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Identity;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class RevokedTokensRepository : RepositoryBase<RevokedToken>, IRevokedTokensRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("token.access_token")
	};
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	public RevokedTokensRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<RevokedTokensRepository> logger) : 
		base(clientProvider, settings, logger, "revoked_tokens")
	{
		
	}
	
	#endregion
}