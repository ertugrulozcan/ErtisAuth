using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ITokenService
	{
		ValueTask<User> WhoAmIAsync(BearerToken bearerToken);
		
		ValueTask<Application> WhoAmIAsync(BasicToken basicToken);
		
		ValueTask<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, string ipAddress = null, string userAgent = null, bool fireEvent = true);

		ValueTask<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true);
		
		ValueTask<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true);
		
		ValueTask<BasicTokenValidationResult> VerifyBasicTokenAsync(string token, bool fireEvent = true);
		
		ValueTask<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true);

		ValueTask<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true);
		
		ValueTask ClearExpiredActiveTokens(string membershipId);
		
		ValueTask ClearRevokedTokens(string membershipId);
	}
}