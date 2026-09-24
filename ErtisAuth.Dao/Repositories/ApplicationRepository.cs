using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Applications;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class ApplicationRepository : RepositoryBase<Application>, IApplicationRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("name"),
		new SingleIndexDefinition("role"),
		new SingleIndexDefinition("membership_id"),
		new CompoundIndexDefinition("_id", "membership_id"),
		new CompoundIndexDefinition("name", "membership_id")
	};
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	public ApplicationRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<ApplicationRepository> logger) : 
		base(clientProvider, settings, logger, "applications")
	{
		
	}
	
	#endregion
}