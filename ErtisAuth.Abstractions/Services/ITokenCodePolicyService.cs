using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Abstractions.Services;

public interface ITokenCodePolicyService : IMembershipBoundedCrudService<TokenCodePolicy>
{
    TokenCodePolicy? GetBySlug(string slug, string membershipId);
    
    ValueTask<TokenCodePolicy?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
}