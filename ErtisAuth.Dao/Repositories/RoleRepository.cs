using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Roles;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("name"),
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
	public RoleRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<RoleRepository> logger) : 
		base(clientProvider, settings, logger, "roles")
	{
		
	}
	
	#endregion
}