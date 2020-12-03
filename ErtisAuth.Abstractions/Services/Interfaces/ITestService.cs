using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ITestService
	{
		Task<IPaginationCollection<TestModel>> GetAsync(string query, int? skip = null, int? limit = null,
			bool? withCount = null, string sortField = null, SortDirection? sortDirection = null);
	}
}