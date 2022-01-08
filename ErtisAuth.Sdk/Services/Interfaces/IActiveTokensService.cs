using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    public interface IActiveTokensService
    {
        IResponseResult<IPaginationCollection<ActiveToken>> GetActiveTokens(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<IResponseResult<IPaginationCollection<ActiveToken>>> GetActiveTokensAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        IResponseResult<IPaginationCollection<ActiveToken>> QueryActiveTokens(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<IResponseResult<IPaginationCollection<ActiveToken>>> QueryActiveTokensAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
    }
}