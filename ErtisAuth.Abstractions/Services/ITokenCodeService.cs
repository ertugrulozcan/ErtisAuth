using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services;

public interface ITokenCodeService : IMembershipBoundedService<TokenCode>
{
    Task<TokenCode> CreateAsync(string membershipId, CancellationToken cancellationToken = default);
    
    Task<TokenCode> AuthorizeCodeAsync(string code, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
    
    Task<BearerToken> GenerateTokenAsync(string code, string membershipId, CancellationToken cancellationToken = default);
}