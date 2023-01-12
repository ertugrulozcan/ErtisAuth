using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
    public interface IMembershipBoundedService<T> : IReadonlyMembershipBoundedService<T>, IDeletableResourceService where T : IHasIdentifier
    {
        IResponseResult<T> Create<TCreateModel>(TCreateModel model, TokenBase token) where TCreateModel : T;
		
        Task<IResponseResult<T>> CreateAsync<TCreateModel>(TCreateModel model, TokenBase token, CancellationToken cancellationToken = default) where TCreateModel : T;
		
        IResponseResult<T> Update(T model, TokenBase token);
		
        Task<IResponseResult<T>> UpdateAsync(T model, TokenBase token, CancellationToken cancellationToken = default);
    }
}