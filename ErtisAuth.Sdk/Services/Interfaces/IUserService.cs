using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IUserService : IMembershipBoundedService<User>
	{
		Task<IResponseResult<IPaginationCollection<T>>> GetAsync<T>(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, Sorting sorting = null, CancellationToken cancellationToken = default) where T : class;
		
		IResponseResult<IPaginationCollection<ActiveToken>> GetActiveTokens(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);

		Task<IResponseResult<IPaginationCollection<ActiveToken>>> GetActiveTokensAsync(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
		
		IResponseResult<IPaginationCollection<RevokedToken>> GetRevokedTokens(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);

		Task<IResponseResult<IPaginationCollection<RevokedToken>>> GetRevokedTokensAsync(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
	}
}