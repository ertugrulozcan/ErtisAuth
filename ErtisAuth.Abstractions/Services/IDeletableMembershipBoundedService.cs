using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IDeletableMembershipBoundedService
{
    bool Delete(Utilizer utilizer, string membershipId, string id);
    
    Task<bool> DeleteAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default);
    
    bool? BulkDelete(Utilizer utilizer, string membershipId, string[] ids);
    
    Task<bool?> BulkDeleteAsync(Utilizer utilizer, string membershipId, string[] ids, CancellationToken cancellationToken = default);
}