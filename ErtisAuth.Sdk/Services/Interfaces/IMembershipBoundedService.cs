using Ertis.Core.Models;
using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

public interface IMembershipBoundedService<T> : IReadonlyMembershipBoundedService<T>, IDeletableResourceService where T : IHasIdentifier
{
	Task<IResponseResult<T>> CreateAsync<TCreateModel>(TCreateModel model, TokenBase token, CancellationToken cancellationToken = default) where TCreateModel : T;
	
	Task<IResponseResult<T>> UpdateAsync(T model, TokenBase token, CancellationToken cancellationToken = default);
}