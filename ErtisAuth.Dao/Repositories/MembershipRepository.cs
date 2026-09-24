using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Memberships;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class MembershipRepository : RepositoryBase<Membership>, IMembershipRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("name")
	};
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	public MembershipRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<MembershipRepository> logger) : 
		base(clientProvider, settings, logger, "memberships")
	{
		
	}
	
	#endregion
}