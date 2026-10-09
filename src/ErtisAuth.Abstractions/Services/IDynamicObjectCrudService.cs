using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IDynamicObjectCrudService
{
    Task<DynamicObject?> GetAsync(string id, CancellationToken cancellationToken = default);
    
    Task<DynamicObject?> FindOneAsync(params IQuery[] queries);
	
    Task<IPaginationCollection<DynamicObject>> GetAsync(
		IEnumerable<IQuery> queries, 
		int? skip = null, 
		int? limit = null, 
		bool withCount = false, 
		string? orderBy = null, 
		SortDirection? sortDirection = null, 
		CancellationToken cancellationToken = default);
	
    Task<DynamicObject> CreateAsync(DynamicObject model, CancellationToken cancellationToken = default);
	
    Task<DynamicObject?> UpdateAsync(DynamicObject model, string id, CancellationToken cancellationToken = default);
	
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
	
    Task<IPaginationCollection<DynamicObject>> QueryAsync(
        string query,
        int? skip = null,
        int? limit = null,
        bool? withCount = null,
        string? orderBy = null,
        SortDirection? sortDirection = null,
        IDictionary<string, bool>? projection = null, 
        string? language = null, 
        CancellationToken cancellationToken = default);
	
    Task<dynamic> AggregateAsync(string aggregationStagesJson, string membershipId, CancellationToken cancellationToken = default);
}