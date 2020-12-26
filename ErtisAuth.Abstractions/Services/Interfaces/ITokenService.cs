using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ITokenService
	{
		Task<User> WhoAmIAsync(string token);
		
		Task<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, bool fireEvent = true);

		Task<TokenValidationResult> VerifyTokenAsync(string token, bool fireEvent = true);
		
		Task<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true);

		Task<bool> RevokeTokenAsync(string token, bool fireEvent = true);

		string CalculatePasswordHash(Membership membership, string password);
	}
}