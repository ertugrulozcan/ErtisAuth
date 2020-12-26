using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IMembershipBoundedService<T> where T : IHasMembership
	{
		Task<IPaginationCollection<dynamic>> QueryAsync(
			string query, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null);
		
		T Get(string membershipId, string id);
		
		Task<T> GetAsync(string membershipId, string id);
		
		IPaginationCollection<T> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);

		Task<IPaginationCollection<T>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
	}
}