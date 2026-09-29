using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using MongoDB.Driver;
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
			// Read with the driver: DynamicMongoRepository.GetIndexesAsync throws on a text index of several fields
			// ("An item with the same key has already been added. Key: text"). The keys of the index definitions
			// are MongoDB's default index names.
			var currentIndexes = await (await this.DocumentCollection.Indexes.ListAsync(cancellationToken)).ToListAsync(cancellationToken);
			var currentIndexNames = currentIndexes.Select(x => x["name"].AsString).ToArray();
			var hasTextIndex = currentIndexes.Any(x => x["key"].AsBsonDocument.Contains("_fts"));
			
			var missingIndexes = new List<IIndexDefinition>();
			foreach (var index in this.Indexes)
			{
				// A collection can have only one text index. An existing one is kept as is: it may have been created
				// by hand under another name, which would never match the definition's key.
				if (index.Type == IndexType.Text && hasTextIndex)
				{
					continue;
				}
				
				if (!currentIndexNames.Contains(index.Key))
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