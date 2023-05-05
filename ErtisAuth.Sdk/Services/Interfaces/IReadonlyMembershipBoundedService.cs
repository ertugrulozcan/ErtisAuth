using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    public interface IReadonlyMembershipBoundedService<T> where T : IHasIdentifier
    {
        IResponseResult<T> Get(string modelId, TokenBase token);

        IResponseResult<TReturn> Get<TReturn>(string modelId, TokenBase token) where TReturn : T;
		
        Task<IResponseResult<T>> GetAsync(string modelId, TokenBase token, CancellationToken cancellationToken = default);
        
        Task<IResponseResult<TReturn>> GetAsync<TReturn>(string modelId, TokenBase token, CancellationToken cancellationToken = default) where TReturn : T;
		
        IResponseResult<IPaginationCollection<T>> Get(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
        Task<IResponseResult<IPaginationCollection<T>>> GetAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null, CancellationToken cancellationToken = default);
		
        IResponseResult<IPaginationCollection<T>> Query(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
        Task<IResponseResult<IPaginationCollection<T>>> QueryAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);
    }
}