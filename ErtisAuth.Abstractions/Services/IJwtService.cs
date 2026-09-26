using System.Text;
using ErtisAuth.Core.Models.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Abstractions.Services;

public interface IJwtService
{
	string GenerateToken(TokenClaims tokenClaims, DateTime? generationTime = null, TimeSpan? expiresIn = null, Encoding? encoding = null);
	
	Task<TokenValidationResult> ValidateTokenAsync(string token, TokenClaims claims, SymmetricSecurityKey secretKey);
	
	JsonWebToken DecodeToken(string token);
	
	bool TryDecodeToken(string token, out JsonWebToken? securityToken);
}