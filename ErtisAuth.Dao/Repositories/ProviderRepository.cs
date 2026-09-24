using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Providers;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class ProviderRepository : RepositoryBase<Provider>, IProviderRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("isActive"),
		new SingleIndexDefinition("membership_id"),
		new CompoundIndexDefinition("isActive", "membership_id")
	};
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	public ProviderRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<ProviderRepository> logger) : 
		base(clientProvider, settings, logger, "providers")
	{
		
	}
	
	#endregion
}