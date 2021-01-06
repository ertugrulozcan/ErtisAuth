using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ITokenService
	{
		Task<User> WhoAmIAsync(string token, string tokenType);
		
		Task<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, bool fireEvent = true);

		Task<ITokenValidationResult> VerifyTokenAsync(string token, string tokenType, bool fireEvent = true);
		
		Task<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true);
		
		Task<BasicTokenValidationResult> VerifyBasicTokenAsync(string token, bool fireEvent = true);
		
		Task<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true);

		Task<bool> RevokeTokenAsync(string token, bool fireEvent = true);
	}
}