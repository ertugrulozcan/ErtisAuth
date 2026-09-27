using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

public interface IRevokedTokenService : IMembershipBoundedService<RevokedToken>
{
	Task RevokeAsync(string token, User user, bool isRefreshToken, CancellationToken cancellationToken = default);
	
	Task<RevokedToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
	
	Task ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default);
}