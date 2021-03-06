using System.Threading.Tasks;
using Ertis.Core.Collections;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IDynamicResourceService
	{
		dynamic GetDynamic(string membershipId, string id);
		
		Task<dynamic> GetDynamicAsync(string membershipId, string id);
		
		IPaginationCollection<dynamic> GetDynamic(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);

		Task<IPaginationCollection<dynamic>> GetDynamicAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);
	}
}