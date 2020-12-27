using System.Threading.Tasks;
using Ertis.Core.Collections;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IGenericCrudService<T>
	{
		T Get(string id);
		
		Task<T> GetAsync(string id);

		IPaginationCollection<T> Get(int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
		
		Task<IPaginationCollection<T>> GetAsync(int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
		
		T Create(T model);
		
		Task<T> CreateAsync(T model);

		T Update(T model);
		
		Task<T> UpdateAsync(T model);

		bool Delete(string id);

		Task<bool> DeleteAsync(string id);
	}
}