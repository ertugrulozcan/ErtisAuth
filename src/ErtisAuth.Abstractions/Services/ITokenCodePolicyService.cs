using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Abstractions.Services;

public interface ITokenCodePolicyService : IMembershipBoundedCrudService<TokenCodePolicy>
{
    Task<TokenCodePolicy?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
}