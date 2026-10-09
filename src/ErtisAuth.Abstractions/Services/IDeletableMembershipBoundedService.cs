using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IDeletableMembershipBoundedService
{
    Task<bool> DeleteAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    Task<bool?> BulkDeleteAsync(string[] ids, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
}