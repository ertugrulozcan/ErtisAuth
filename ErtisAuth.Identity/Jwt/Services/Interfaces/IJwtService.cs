using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Ertis.Security.Cryptography;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Identity.Jwt.Services.Interfaces
{
	public interface IJwtService
	{
		string GenerateToken(TokenClaims tokenClaims, HashAlgorithms hashAlgorithm, Encoding encoding);

		string GenerateToken(TokenClaims tokenClaims, DateTime tokenGenerationTime, HashAlgorithms hashAlgorithm, Encoding encoding);
		
		JwtSecurityToken DecodeToken(string token);
		
		bool TryDecodeToken(string token, out JwtSecurityToken securityToken);
	}
}