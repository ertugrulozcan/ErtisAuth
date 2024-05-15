using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services
{
	public interface IActiveTokenService : IMembershipBoundedService<ActiveToken>
	{
		Task<ActiveToken> CreateAsync(
			BearerToken token,
			User user,
			string membershipId,
			string ipAddress = null,
			string userAgent = null,
			CancellationToken cancellationToken = default);

		Task<ActiveToken> GetByAccessTokenAsync(
			string accessToken,
			CancellationToken cancellationToken = default);

		Task<ActiveToken> GetByRefreshTokenAsync(
			string refreshToken,
			CancellationToken cancellationToken = default);

		Task<IEnumerable<ActiveToken>> GetActiveTokensByUser(
			string userId, 
			string membershipId,
			CancellationToken cancellationToken = default);

		Task BulkDeleteAsync(IEnumerable<ActiveToken> activeTokens, CancellationToken cancellationToken = default);
		
		ValueTask ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default);
	}
}