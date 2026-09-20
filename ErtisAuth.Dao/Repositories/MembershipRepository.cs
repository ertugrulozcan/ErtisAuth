using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;

namespace ErtisAuth.Dao.Repositories;

public class MembershipRepository : RepositoryBase<MembershipDto>, IMembershipRepository
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
	public MembershipRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings) : 
		base(clientProvider, settings, "memberships")
	{
		
	}
	
	#endregion
}