using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services.Interfaces;

public interface ITokenCodePolicyService : IMembershipBoundedCrudService<TokenCodePolicy>
{
    TokenCodePolicy GetBySlug(string slug, string membershipId);
    
    ValueTask<TokenCodePolicy> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
}