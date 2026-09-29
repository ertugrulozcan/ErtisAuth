using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.Dao.Repositories;

public class UserRepository : DynamicRepositoryBase, IUserRepository
{
    #region Properties
    
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("username"),
        new SingleIndexDefinition("email_address"),
        new SingleIndexDefinition("membership_id"),
        new CompoundIndexDefinition("_id", "membership_id"),
        new CompoundIndexDefinition("username", "membership_id"),
        new CompoundIndexDefinition("email_address", "membership_id"),
        new TextIndexDefinition(["username", "firstname", "lastname", "email_address"], IndexLocale.none)
    };
    
    #endregion
    
    #region Constructor
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public UserRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<UserRepository> logger) : 
        base(clientProvider, settings, logger, "users")
    {
        
    }
    
    #endregion
	
	#region Unique Index Methods
	
	public async Task<string[]> GetIndexNamesAsync(string prefix, CancellationToken cancellationToken = default)
	{
		var indexes = await (await this.DocumentCollection.Indexes.ListAsync(cancellationToken)).ToListAsync(cancellationToken);
		return indexes.Select(x => x["name"].AsString).Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
	}
	
	public async Task CreateUniqueIndexAsync(string name, string path, BsonDocument partialFilterExpression, CancellationToken cancellationToken = default)
	{
		var keys = Builders<BsonDocument>.IndexKeys.Ascending("membership_id").Ascending(path);
		var options = new CreateIndexOptions<BsonDocument>
		{
			Name = name,
			Unique = true,
			PartialFilterExpression = partialFilterExpression
		};
		
		await this.DocumentCollection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys, options), cancellationToken: cancellationToken);
	}
	
	public async Task DropIndexAsync(string name, CancellationToken cancellationToken = default)
	{
		await this.DocumentCollection.Indexes.DropOneAsync(name, cancellationToken);
	}
	
	#endregion
}