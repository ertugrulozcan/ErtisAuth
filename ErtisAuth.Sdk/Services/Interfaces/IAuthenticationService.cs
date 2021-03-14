using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IAuthenticationService
	{
		#region Methods

		IResponseResult<BearerToken> GetToken(string username, string password);
		
		Task<IResponseResult<BearerToken>> GetTokenAsync(string username, string password);
		
		IResponseResult<BearerToken> RefreshToken(BearerToken token);
		
		Task<IResponseResult<BearerToken>> RefreshTokenAsync(BearerToken token);
		
		IResponseResult<BearerToken> RefreshToken(string refreshToken);
		
		Task<IResponseResult<BearerToken>> RefreshTokenAsync(string refreshToken);
		
		IResponseResult<BearerToken> VerifyToken(BearerToken token);
		
		Task<IResponseResult<BearerToken>> VerifyTokenAsync(BearerToken token);
		
		IResponseResult<BearerToken> VerifyToken(string accessToken);
		
		Task<IResponseResult<BearerToken>> VerifyTokenAsync(string accessToken);
		
		IResponseResult RevokeToken(BearerToken token);
		
		Task<IResponseResult> RevokeTokenAsync(BearerToken token);
		
		IResponseResult RevokeToken(string accessToken);
		
		Task<IResponseResult> RevokeTokenAsync(string accessToken);

		IResponseResult<User> WhoAmI(BearerToken bearerToken);
		
		Task<IResponseResult<User>> WhoAmIAsync(BearerToken bearerToken);
		
		IResponseResult<Application> WhoAmI(BasicToken basicToken);
		
		Task<IResponseResult<Application>> WhoAmIAsync(BasicToken basicToken);

		#endregion
	}
}