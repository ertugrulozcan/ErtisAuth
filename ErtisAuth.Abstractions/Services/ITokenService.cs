using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services
{
	public interface ITokenService
	{
		ValueTask<User> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default);
		
		ValueTask<Application> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default);
		
		ValueTask<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, string ipAddress = null, string userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default);

		ValueTask<BearerToken> GenerateTokenAsync(User user, string membershipId, string ipAddress = null, string userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default);

		ValueTask<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true, CancellationToken cancellationToken = default);
		
		ValueTask<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true, CancellationToken cancellationToken = default);
		
		ValueTask<BasicTokenValidationResult> VerifyBasicTokenAsync(string token, bool fireEvent = true, CancellationToken cancellationToken = default);
		
		ValueTask<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true, CancellationToken cancellationToken = default);

		ValueTask<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true, CancellationToken cancellationToken = default);
		
		ValueTask RevokeAllAsync(string membershipId, string userId, bool fireEvent = true, CancellationToken cancellationToken = default);
		
		ValueTask ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default);
		
		ValueTask ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default);

		Task<User> GetTokenOwnerUserAsync(string bearerToken, CancellationToken cancellationToken = default);
	}
}