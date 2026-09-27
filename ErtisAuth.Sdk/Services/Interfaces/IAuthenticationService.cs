using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

[ServiceLifetime(ServiceLifetime.Singleton)]
public interface IAuthenticationService
{
	#region Methods
	
	Task<IResponseResult<BearerToken>> GetTokenAsync(string username, string password, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<BearerToken>> RefreshTokenAsync(BearerToken token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<BearerToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(BearerToken token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(string accessToken, CancellationToken cancellationToken = default);
	
	Task<IResponseResult> RevokeTokenAsync(BearerToken token, bool logoutFromAllDevices = false, CancellationToken cancellationToken = default);
	
	Task<IResponseResult> RevokeTokenAsync(string accessToken, bool logoutFromAllDevices = false, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<User>> MeAsync(BearerToken bearerToken, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<T>> MeAsync<T>(BearerToken bearerToken, CancellationToken cancellationToken = default) where T : class;
	
	Task<IResponseResult<User>> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<T>> WhoAmIAsync<T>(BearerToken bearerToken, CancellationToken cancellationToken = default) where T : class;
	
	Task<IResponseResult<Application>> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default);
	
	#endregion
}