using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IMembershipBoundedService
	{
		TModel Get<TModel>(string membershipId, string id) where TModel : class, IHasMembership;
		
		ValueTask<TModel> GetAsync<TModel>(string membershipId, string id) where TModel : class, IHasMembership;
		
		IPaginationCollection<TModel> Get<TModel>(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection) where TModel : class, IHasMembership;

		ValueTask<IPaginationCollection<TModel>> GetAsync<TModel>(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection) where TModel : class, IHasMembership;
	}
	
	public interface IMembershipBoundedService<TModel> : IMembershipBoundedService where TModel : IHasMembership
	{
		IPaginationCollection<dynamic> Query(
			string query, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null);
		
		ValueTask<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null);

		TModel Get(string membershipId, string id);
		
		ValueTask<TModel> GetAsync(string membershipId, string id);
		
		IPaginationCollection<TModel> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);

		ValueTask<IPaginationCollection<TModel>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
		
		IPaginationCollection<TModel> Search(
			string membershipId, 
			string keyword, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null);
		
		ValueTask<IPaginationCollection<TModel>> SearchAsync(
			string membershipId, 
			string keyword, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null);

		dynamic Aggregate(string aggregateStagesJson);

		Task<dynamic> AggregateAsync(string aggregateStagesJson);
	}
}