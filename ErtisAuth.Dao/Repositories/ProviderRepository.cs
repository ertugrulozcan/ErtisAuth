using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dao.Serialization;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Core;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

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
	
	#region Read Methods
	
	public async Task<Provider?> FindOneByTypeAsync(ProviderType type, string membershipId, CancellationToken cancellationToken = default)
	{
		var filter = 
			Builders<Provider>.Filter.Eq(ProviderDiscriminatorConvention.ElementName, type.ToString()) & 
			Builders<Provider>.Filter.Eq(x => x.MembershipId, membershipId);
		
		return await this.Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
	}
	
	#endregion
}