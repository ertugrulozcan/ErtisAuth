using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public abstract class DynamicRepositoryBase : DynamicMongoRepository, IRepositoryBase
{
	#region Services
	
	private readonly ILogger<DynamicRepositoryBase> _logger;
	
	#endregion
	
	#region Properties
	
	protected virtual IIndexDefinition[] Indexes => Array.Empty<IIndexDefinition>();
	
	#endregion
    
	#region Constructors
    
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	/// <param name="collectionName"></param>
	protected DynamicRepositoryBase(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<DynamicRepositoryBase> logger, 
		string collectionName) : 
		base(clientProvider, settings, collectionName)
	{
		this._logger = logger;
	}
	
	#endregion
	
	#region Index Methods
	
	public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
	{
		if (!this.Indexes.Any())
		{
			return;
		}
        
		try
		{
			var currentIndexes = (await this.GetIndexesAsync(cancellationToken)).ToArray();
			var missingIndexes = new List<IIndexDefinition>();
			foreach (var index in this.Indexes)
			{
				if (currentIndexes.All(x => x.Key != index.Key))
				{
					missingIndexes.Add(index);
				}
			}
			
			if (missingIndexes.Any())
			{
				await this.CreateManyIndexAsync(missingIndexes, cancellationToken);
				
				foreach (var index in missingIndexes)
				{
					this._logger.LogInformation("Index '{Key}' created on {CollectionName} collection", index.Key, this.CollectionName);
				}
			}
			else
			{
				this._logger.LogInformation("All indexes already exist on {CollectionName} collection", this.CollectionName);
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "DynamicRepositoryBase.CreateIndexesAsync occured an error");
		}
	}
	
	#endregion
}