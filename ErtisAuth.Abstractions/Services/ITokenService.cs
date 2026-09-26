using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

// ReSharper disable UnusedParameter.Global
namespace ErtisAuth.Abstractions.Services;

public interface ITokenService
{
	Task<User?> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default);
	
	Task<Application?> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default);
	
	Task<BearerToken> GenerateTokenAsync(string username, string password, string membershipId, string? ipAddress = null, string? userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task<ScopedBearerToken> GenerateTokenAsync(string token, string[]? scopes, string membershipId, CancellationToken cancellationToken = default);
	
	Task<BearerToken> GenerateTokenAsync(User user, string membershipId, string? ipAddress = null, string? userAgent = null, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task<ITokenValidationResult> VerifyTokenAsync(string token, SupportedTokenTypes tokenType, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task<BearerTokenValidationResult> VerifyBearerTokenAsync(string token, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task<BasicTokenValidationResult> VerifyBasicTokenAsync(string token, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task<BearerToken> RefreshTokenAsync(string refreshToken, bool revokeBefore = true, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task<bool> RevokeTokenAsync(string token, bool logoutFromAllDevices = false, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task RevokeAllAsync(string membershipId, string userId, bool fireEvent = true, CancellationToken cancellationToken = default);
	
	Task ClearExpiredActiveTokens(string membershipId, CancellationToken cancellationToken = default);
	
	Task ClearRevokedTokens(string membershipId, CancellationToken cancellationToken = default);
	
	Task<User?> GetTokenOwnerUserAsync(string bearerToken, CancellationToken cancellationToken = default);
}