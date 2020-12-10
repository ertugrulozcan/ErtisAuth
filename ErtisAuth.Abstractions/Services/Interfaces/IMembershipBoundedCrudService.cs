using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IMembershipBoundedCrudService<T> where T : IHasMembership
	{
		T Get(string membershipId, string id);
		
		Task<T> GetAsync(string membershipId, string id);
		IPaginationCollection<T> Get(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);

		Task<IPaginationCollection<T>> GetAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
		
		T Create(string membershipId, T model);
		
		Task<T> CreateAsync(string membershipId, T model);
	}
}