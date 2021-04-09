using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Providers;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IProviderService
	{
		IResponseResult<OAuthProvider> GetProvider(string roleId, TokenBase token);
		
		Task<IResponseResult<OAuthProvider>> GetProviderAsync(string roleId, TokenBase token);
		
		IResponseResult<IPaginationCollection<OAuthProvider>> GetProviders(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<OAuthProvider>>> GetProvidersAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		IResponseResult<IPaginationCollection<OAuthProvider>> QueryProviders(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<OAuthProvider>>> QueryProvidersAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
	}
}