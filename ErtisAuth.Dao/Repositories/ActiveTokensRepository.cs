using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class ActiveTokensRepository : RepositoryBase<ActiveToken>, IActiveTokensRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("user_id"),
		new SingleIndexDefinition("username"),
		new SingleIndexDefinition("email_address"),
		new SingleIndexDefinition("membership_id"),
		new CompoundIndexDefinition("user_id", "membership_id"),
		new CompoundIndexDefinition("expire_time", "membership_id")
	};
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	public ActiveTokensRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings, 
		ILogger<ActiveTokensRepository> logger) : 
		base(clientProvider, settings, logger, "active_tokens")
	{
		
	}
	
	#endregion
}