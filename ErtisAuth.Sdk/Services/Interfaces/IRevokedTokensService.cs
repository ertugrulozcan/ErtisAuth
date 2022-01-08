using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    public interface IRevokedTokensService
    {
        IResponseResult<IPaginationCollection<RevokedToken>> GetRevokedTokens(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<IResponseResult<IPaginationCollection<RevokedToken>>> GetRevokedTokensAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        IResponseResult<IPaginationCollection<RevokedToken>> QueryRevokedTokens(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<IResponseResult<IPaginationCollection<RevokedToken>>> QueryRevokedTokensAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
    }
}