using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ITokenService
	{
		Task<User> WhoAmIAsync(BearerToken bearerToken);
		
		Task<Application> WhoAmIAsync(BasicToken basicToken);
		
		Task<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, bool fireEvent = true);

		Task<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true);
		
		Task<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true);
		
		Task<BasicTokenValidationResult> VerifyBasicTokenAsync(string token, bool fireEvent = true);
		
		Task<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true);

		Task<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true);

		Task ClearExpiredActiveTokens(string membershipId);
		
		Task ClearRevokedTokens(string membershipId);
	}
}