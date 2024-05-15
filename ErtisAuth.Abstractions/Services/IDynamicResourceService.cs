using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;

namespace ErtisAuth.Abstractions.Services
{
	public interface IDynamicResourceService
	{
		dynamic GetDynamic(string membershipId, string id);
		
		ValueTask<dynamic> GetDynamicAsync(string membershipId, string id, CancellationToken cancellationToken = default);
		
		IPaginationCollection<dynamic> GetDynamic(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection);

		ValueTask<IPaginationCollection<dynamic>> GetDynamicAsync(string membershipId, int? skip, int? limit, bool withCount, string orderBy, SortDirection? sortDirection, CancellationToken cancellationToken = default);
	}
}