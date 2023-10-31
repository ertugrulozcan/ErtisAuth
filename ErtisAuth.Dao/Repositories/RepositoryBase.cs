using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Data.Models;
using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;

namespace ErtisAuth.Dao.Repositories;

public abstract class RepositoryBase<TDto> : MongoRepositoryBase<TDto>, IRepositoryBase where TDto : IEntity<string>
{
	#region Properties
	
	// ReSharper disable once ReturnTypeCanBeEnumerable.Global
	protected virtual IIndexDefinition[] Indexes => Array.Empty<IIndexDefinition>();

	#endregion
    
	#region Constructors
    
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="collectionName"></param>
	/// <param name="actionBinder"></param>
	protected RepositoryBase(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings, 
		string collectionName, 
		IRepositoryActionBinder actionBinder = null) : 
		base(clientProvider, settings, collectionName, actionBinder)
	{
		
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
					Console.WriteLine($"Index '{index.Key}' created on {this.CollectionName} collection.");
				}
			}
			else
			{
				Console.WriteLine($"All indexes already exist on {this.CollectionName} collection.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
		}
	}

	#endregion
}