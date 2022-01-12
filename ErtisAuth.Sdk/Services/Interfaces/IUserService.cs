using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IUserService : IMembershipBoundedService<User>
	{
		IResponseResult<IPaginationCollection<ActiveToken>> GetActiveTokens(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);

		Task<IResponseResult<IPaginationCollection<ActiveToken>>> GetActiveTokensAsync(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		IResponseResult<IPaginationCollection<RevokedToken>> GetRevokedTokens(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);

		Task<IResponseResult<IPaginationCollection<RevokedToken>>> GetRevokedTokensAsync(string userId, TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
	}
}