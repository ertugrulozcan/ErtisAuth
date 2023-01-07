using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.MongoDB.Queries;
using Ertis.Schema.Dynamics;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
    public interface IDynamicObjectCrudService
    {
        Task<DynamicObject> GetAsync(string id);
	    
        Task<DynamicObject> FindOneAsync(params IQuery[] queries);
		
        Task<IPaginationCollection<DynamicObject>> GetAsync(IEnumerable<IQuery> queries, int? skip = null, int? limit = null, bool withCount = false, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<DynamicObject> CreateAsync(DynamicObject model);
		
        Task<DynamicObject> UpdateAsync(string id, DynamicObject model);

        Task<bool> DeleteAsync(string id);
		
        Task<IPaginationCollection<DynamicObject>> QueryAsync(
            string query,
            int? skip = null,
            int? limit = null,
            bool? withCount = null,
            string orderBy = null,
            SortDirection? sortDirection = null,
            IDictionary<string, bool> selectFields = null);

        dynamic Aggregate(string membershipId, string aggregationStagesJson);

        Task<dynamic> AggregateAsync(string membershipId, string aggregationStagesJson);
    }
}