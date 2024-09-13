using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics.Legacy;

namespace ErtisAuth.Abstractions.Services
{
    public interface IDynamicObjectCrudService
    {
        Task<DynamicObject> GetAsync(string id, CancellationToken cancellationToken = default);
	    
        Task<DynamicObject> FindOneAsync(params IQuery[] queries);
		
        Task<IPaginationCollection<DynamicObject>> GetAsync(IEnumerable<IQuery> queries, int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
		
        Task<DynamicObject> CreateAsync(DynamicObject model, CancellationToken cancellationToken = default);
		
        Task<DynamicObject> UpdateAsync(string id, DynamicObject model, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
		
        Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string query,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null,
            IDictionary<string, bool> selectFields = null, 
            CancellationToken cancellationToken = default);

        dynamic Aggregate(string membershipId, string aggregationStagesJson);

        Task<dynamic> AggregateAsync(string membershipId, string aggregationStagesJson, CancellationToken cancellationToken = default);
    }
}