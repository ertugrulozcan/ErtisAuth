using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IAuthenticationService
	{
		#region Methods

		IResponseResult<BearerToken> GetToken(string username, string password, string ipAddress = null, string userAgent = null);
		
		Task<IResponseResult<BearerToken>> GetTokenAsync(string username, string password, string ipAddress = null, string userAgent = null, CancellationToken cancellationToken = default);
		
		IResponseResult<BearerToken> RefreshToken(BearerToken token);
		
		Task<IResponseResult<BearerToken>> RefreshTokenAsync(BearerToken token, CancellationToken cancellationToken = default);
		
		IResponseResult<BearerToken> RefreshToken(string refreshToken);
		
		Task<IResponseResult<BearerToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
		
		IResponseResult<ITokenValidationResult> VerifyToken(BearerToken token);
		
		Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(BearerToken token, CancellationToken cancellationToken = default);
		
		IResponseResult<ITokenValidationResult> VerifyToken(string accessToken);
		
		Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(string accessToken, CancellationToken cancellationToken = default);
		
		IResponseResult RevokeToken(BearerToken token, bool logoutFromAllDevices = false);
		
		Task<IResponseResult> RevokeTokenAsync(BearerToken token, bool logoutFromAllDevices = false, CancellationToken cancellationToken = default);
		
		IResponseResult RevokeToken(string accessToken, bool logoutFromAllDevices = false);
		
		Task<IResponseResult> RevokeTokenAsync(string accessToken, bool logoutFromAllDevices = false, CancellationToken cancellationToken = default);

		IResponseResult<User> WhoAmI(BearerToken bearerToken);
		
		Task<IResponseResult<User>> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default);
		
		IResponseResult<Application> WhoAmI(BasicToken basicToken);
		
		Task<IResponseResult<Application>> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default);

		#endregion
	}
}