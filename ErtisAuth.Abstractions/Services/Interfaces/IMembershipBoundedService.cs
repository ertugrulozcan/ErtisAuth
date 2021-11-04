using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IMembershipBoundedService<TModel> where TModel : IHasMembership
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
	}
}