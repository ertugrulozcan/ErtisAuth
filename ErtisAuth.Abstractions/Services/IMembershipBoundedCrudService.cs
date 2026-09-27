using ErtisAuth.Core.Models;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IMembershipBoundedCrudService<T> : IMembershipBoundedService<T>, IDeletableMembershipBoundedService where T : IHasMembership
{
	Task<T> CreateAsync(Utilizer utilizer, string membershipId, T model, CancellationToken cancellationToken = default);
	
	Task<T> UpdateAsync(Utilizer utilizer, string membershipId, T model, CancellationToken cancellationToken = default);
}