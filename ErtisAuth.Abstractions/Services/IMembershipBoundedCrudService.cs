using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IMembershipBoundedCrudService<T> : IMembershipBoundedService<T>, IDeletableMembershipBoundedService where T : IHasMembership
{
	Task<T> CreateAsync(T model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
	
	Task<T> UpdateAsync(T model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
}