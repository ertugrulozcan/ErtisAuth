using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

public interface IRevokedTokenService : IMembershipBoundedService<RevokedToken>
{
	/// <param name="token"></param>
	/// <param name="user"></param>
	/// <param name="isRefreshToken"></param>
	/// <param name="retainUntil">The expiry of the revoked token; the revocation record is kept until then</param>
	/// <param name="cancellationToken"></param>
	Task RevokeAsync(string token, User user, bool isRefreshToken, DateTime retainUntil, CancellationToken cancellationToken = default);
	
	Task<RevokedToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

}