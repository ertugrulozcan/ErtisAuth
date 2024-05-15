using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services
{
	public interface IRevokedTokenService : IMembershipBoundedService<RevokedToken>
	{
		Task RevokeAsync(ActiveToken activeToken, User user, bool isRefreshToken, CancellationToken cancellationToken = default);
		
		Task<RevokedToken> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

		ValueTask ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default);
	}
}