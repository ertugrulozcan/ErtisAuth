using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services.Interfaces;

public interface ITokenCodeService : IMembershipBoundedService<TokenCode>
{
    Task<TokenCode> CreateAsync(string membershipId, CancellationToken cancellationToken = default);

    ValueTask ClearExpiredTokenCodes(string membershipId, CancellationToken cancellationToken = default);
}