using Ertis.Core.Collections;
using Ertis.MongoDB.Models;
using Ertis.MongoDB.Queries;
using Ertis.MongoDB.Repository;
using Ertis.Schema.Dynamics;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Helpers;
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace ErtisAuth.Infrastructure.Services;

public partial class DynamicObjectCrudService : IDynamicObjectCrudService
{
	#region Services
	
	private readonly IDynamicMongoRepository _repository;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="repository"></param>
	protected DynamicObjectCrudService(IDynamicMongoRepository repository)
	{
		this._repository = repository;
	}
	
	#endregion
	
	#region Read Methods
    
    public virtual async Task<DynamicObject?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await this._repository.FindOneAsync(id, cancellationToken: cancellationToken);
        return item == null ? null : new DynamicObject(item);
    }
    
    public virtual async Task<DynamicObject?> FindOneAsync(params IQuery[] queries)
    {
        var query = QueryBuilder.Where(queries);
        var matches = await this._repository.FindAsync(query.ToString(), sorting: null);
        var item = matches.Items.FirstOrDefault();
        return item == null ? null : new DynamicObject(item);
    }
    
    public virtual async Task<IPaginationCollection<DynamicObject>> GetAsync(
        IEnumerable<IQuery> queries,
        int? skip = null, 
        int? limit = null, 
        bool withCount = false, 
        string? orderBy = null,
        SortDirection? sortDirection = null, 
        CancellationToken cancellationToken = default)
    {
        var query = QueryBuilder.Where(queries);
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
        var paginatedCollection = await this._repository.FindAsync(query.ToString(), skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
        return new PaginationCollection<DynamicObject>
        {
            Count = paginatedCollection.Count,
            Items = paginatedCollection.Items.Select(x => new DynamicObject(x))
        };
    }
    
    public virtual async Task<IPaginationCollection<DynamicObject>> QueryAsync(
        string query, 
        int? skip = null, 
        int? limit = null, 
        bool? withCount = null, 
        string? orderBy = null,
        SortDirection? sortDirection = null, 
        IDictionary<string, bool>? selectFields = null, 
        string? language = null,  
        CancellationToken cancellationToken = default)
    {
        Locale? locale = Enum.TryParse<Locale>(language, out var locale_) ? locale_ : null;
        var collationOptions = locale != null ? new CollationOptions { Locale = locale } : null;
		limit ??= Constants.PaginationDefaults.MAX_LIMIT;
        var paginatedCollection = await this._repository.QueryAsync(query, skip, limit, withCount, orderBy, sortDirection, selectFields, null, collationOptions, cancellationToken: cancellationToken);
        return new PaginationCollection<DynamicObject>
        {
            Count = paginatedCollection.Count,
            Items = paginatedCollection.Items.Select(x => new DynamicObject(x))
        };
    }
    
    #endregion
	
	#region Create Methods
	
	public virtual async Task<DynamicObject> CreateAsync(DynamicObject model, CancellationToken cancellationToken = default)
	{
		try
		{
			var bsonDocument = BsonDocument.Create(model.ToDynamic());
			var insertedDocument = await this._repository.InsertAsync(bsonDocument, cancellationToken: cancellationToken) as BsonDocument;
			return DynamicObject.Create((Dictionary<string, object?>) BsonTypeMapper.MapToDotNetValue(insertedDocument));
		}
		catch (MongoDB.Driver.MongoWriteException ex)
		{
			if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
			{
				throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message, GetIndexName(ex.WriteError.Message));
			}
			
			throw;
		}
	}
	
	#endregion
	
	#region Update Methods
	
	public virtual async Task<DynamicObject?> UpdateAsync(DynamicObject model, string id, CancellationToken cancellationToken = default)
	{
		try
		{
			var bsonDocument = BsonDocument.Create(model.ToDynamic());
			var updatedDocument = await this._repository.UpdateAsync(bsonDocument, id, cancellationToken: cancellationToken);
			return updatedDocument != null ? await this.GetAsync(id, cancellationToken: cancellationToken) : null;
		}
		catch (MongoDB.Driver.MongoWriteException ex)
		{
			if (ex.WriteError.Category == MongoDB.Driver.ServerErrorCategory.DuplicateKey)
			{
				throw ErtisAuthException.DuplicateKeyError(ex.WriteError.Message, GetIndexName(ex.WriteError.Message));
			}
			
			throw;
		}
	}
	
	/// <summary>
	/// The violated index in the duplicate key error message (e.g. "E11000 duplicate key error collection: db.users index: ux_email_address dup key: { ... }").
	/// </summary>
	private static string? GetIndexName(string message)
	{
		var match = DuplicateKeyIndexNameRegex().Match(message);
		return match.Success ? match.Groups["name"].Value : null;
	}
	
	[GeneratedRegex(@" index: (?<name>\S+) dup key:")]
	private static partial Regex DuplicateKeyIndexNameRegex();
	
	#endregion
	
	#region Delete Methods
	
	public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var isDeleted = await this._repository.DeleteAsync(id, cancellationToken: cancellationToken);
		return isDeleted;
	}
	
	#endregion
	
	#region Aggregation Methods
	
	public async Task<dynamic> AggregateAsync(string aggregationStagesJson, string membershipId, CancellationToken cancellationToken = default)
	{
		return await this._repository.AggregateAsync(QueryHelper.InjectMembershipIdToAggregation(aggregationStagesJson, membershipId), cancellationToken: cancellationToken);
	}
	
	#endregion
}