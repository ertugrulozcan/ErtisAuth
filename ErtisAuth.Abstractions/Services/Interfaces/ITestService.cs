using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Core.Models;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ITestService
	{
		Task<IPaginationCollection<dynamic>> GetAsync(
			string query, 
			int? skip = null, 
			int? limit = null,
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null);
	}
}