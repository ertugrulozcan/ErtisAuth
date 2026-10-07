using System.Text;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Abstractions.Services;

public interface IJwtService
{
	string GenerateToken(TokenClaims tokenClaims, DateTime? generationTime = null, TimeSpan? expiresIn = null, Encoding? encoding = null);
	
	Task<TokenValidationResult> ValidateTokenAsync(string token, TokenClaims claims, SymmetricSecurityKey secretKey);
	
	Task<TokenValidationResult> ValidateTokenAsync(string token, Membership membership);
	
	/// <summary>
	/// Validates a reset password or activation token: signed with the membership key, not expired, of the expected type,
	/// issued for the membership and for a subject. Throws InvalidToken (TokenWasExpired when expired).
	/// </summary>
	Task<JsonWebToken> ValidateActionTokenAsync(string token, Membership membership, string expectedTokenType);
	
	JsonWebToken DecodeToken(string token);
	
	bool TryDecodeToken(string token, out JsonWebToken? securityToken);
}