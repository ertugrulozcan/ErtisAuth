using Ertis.MongoDB.Repository;
using MongoDB.Bson;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IUserRepository : IDynamicMongoRepository
{
	/// <summary>
	/// Names of the indexes starting with the prefix.
	/// </summary>
	Task<string[]> GetIndexNamesAsync(string prefix, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Creates a unique index on { membership_id, path } covering only the documents matching the partial filter.
	/// Throws MongoCommandException (code 11000) when the covered documents already have duplicates.
	/// </summary>
	Task CreateUniqueIndexAsync(string name, string path, BsonDocument partialFilterExpression, CancellationToken cancellationToken = default);
	
	Task DropIndexAsync(string name, CancellationToken cancellationToken = default);
}
