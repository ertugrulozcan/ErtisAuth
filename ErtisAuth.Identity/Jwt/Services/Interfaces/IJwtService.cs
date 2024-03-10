using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Ertis.Security.Cryptography;
using ErtisAuth.Core.Models.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Identity.Jwt.Services.Interfaces
{
	public interface IJwtService
	{
		string GenerateToken(TokenClaims tokenClaims, HashAlgorithms hashAlgorithm, Encoding encoding, TimeSpan? expiresIn = null);

		string GenerateToken(TokenClaims tokenClaims, DateTime tokenGenerationTime, HashAlgorithms hashAlgorithm, Encoding encoding);

		bool ValidateToken(string token, TokenClaims claims, SymmetricSecurityKey secretKey, out SecurityToken validatedToken);
		
		JwtSecurityToken DecodeToken(string token);
		
		bool TryDecodeToken(string token, out JwtSecurityToken securityToken);
	}
}