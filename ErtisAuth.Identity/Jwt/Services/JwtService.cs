using System;
using System.Collections.Generic;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ertis.Security.Cryptography;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Identity.Jwt.Services
{
	public class JwtService : IJwtService
    {
        #region Methods

        public string GenerateToken(TokenClaims tokenClaims, HashAlgorithms hashAlgorithm, Encoding encoding)
        {
            return this.GenerateToken(
                hashAlgorithm,
                encoding,
                DateTime.Now, 
                tokenClaims.SecretKey,
                tokenClaims.Issuer,
                tokenClaims.Audience,
                tokenClaims.ExpiresIn,
                tokenClaims.Subject,
                tokenClaims.TokenId,
                tokenClaims.Principal,
                tokenClaims.FirstName,
                tokenClaims.LastName,
				tokenClaims.Username,
                tokenClaims.EmailAddress,
                tokenClaims.AdditionalClaims);
        }
        
        public string GenerateToken(TokenClaims tokenClaims, DateTime tokenGenerationTime, HashAlgorithms hashAlgorithm, Encoding encoding)
        {
            return this.GenerateToken(
                hashAlgorithm,
                encoding,
                tokenGenerationTime, 
                tokenClaims.SecretKey,
                tokenClaims.Issuer,
                tokenClaims.Audience,
                tokenClaims.ExpiresIn,
                tokenClaims.Subject,
                tokenClaims.TokenId,
                tokenClaims.Principal,
                tokenClaims.FirstName,
                tokenClaims.LastName,
                tokenClaims.Username,
				tokenClaims.EmailAddress,
                tokenClaims.AdditionalClaims);
        }

        private string GenerateToken(
            HashAlgorithms hashAlgorithm,
            Encoding encoding,
            DateTime tokenGenerationTime,
            string secretKey, 
            string issuer, 
            string audience, 
            TimeSpan expirationTime,
            string subject = null,
            string tokenId = null,
            string principal = null,
            string firstName = null,
            string lastName = null,
            string username = null,
            string email = null,
            IDictionary<string, object> additionalClaims = null)
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentException("SecretKey is required field!");
            }
            
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("Issuer is required field!");
            }
            
            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException("Audience is required field!");
            }

            var expireTime = tokenGenerationTime.Add(expirationTime);
            var timestamp = new DateTimeOffset(tokenGenerationTime).ToUnixTimeSeconds();
            var securityKey = new SymmetricSecurityKey(encoding.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, GetSecurityAlgorithmTag(hashAlgorithm));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Azp, audience),
                new Claim(JwtRegisteredClaimNames.Iat, timestamp.ToString()),
            };

            if (!string.IsNullOrEmpty(subject))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
            }

            if (!string.IsNullOrEmpty(tokenId))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenId));
            }

            if (!string.IsNullOrEmpty(principal))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Prn, principal));
            }

            if (!string.IsNullOrEmpty(firstName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, firstName));
            }

            if (!string.IsNullOrEmpty(lastName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, lastName));
            }

            if (!string.IsNullOrEmpty(username))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, username));
            }
			
			if (!string.IsNullOrEmpty(email))
			{
				claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
			}

            if (additionalClaims != null)
            {
                foreach (var additionalClaim in additionalClaims)
                {
                    if (!claims.Exists(x => x.Type == additionalClaim.Key) && additionalClaim.Value != null)
                    {
                        claims.Add(new Claim(additionalClaim.Key, additionalClaim.Value.ToString() ?? string.Empty));
                    }
                }
            }

            var token = new JwtSecurityToken(issuer, audience, claims, notBefore: null, expires: expireTime, signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        public bool ValidateToken(string token, TokenClaims claims, SymmetricSecurityKey secretKey, out SecurityToken validatedToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var result = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = claims.Issuer,
                    ValidAudience = claims.Audience,
                    IssuerSigningKey = secretKey,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                }, out validatedToken);
                
                return result != null;
            }
            catch
            {
                validatedToken = null;
                return false;
            }
        }

        public JwtSecurityToken DecodeToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }

        public bool TryDecodeToken(string token, out JwtSecurityToken securityToken)
        {
            try
            {
                securityToken = this.DecodeToken(token);
                return true;
            }
            catch
            {
                securityToken = null;
                return false;
            }
        }
        
        private string GetSecurityAlgorithmTag(HashAlgorithms hashAlgorithm)
        {
            switch (hashAlgorithm)
            {
                case HashAlgorithms.MD5:
                    return SecurityAlgorithms.Ripemd160Digest;
                case HashAlgorithms.SHA0:
                    return SecurityAlgorithms.Sha256;
                case HashAlgorithms.SHA1:
                    return SecurityAlgorithms.Sha256;
                case HashAlgorithms.SHA2_224:
                    return SecurityAlgorithms.HmacSha256;
                case HashAlgorithms.SHA2_256:
                    return SecurityAlgorithms.HmacSha256;
                case HashAlgorithms.SHA2_384:
                    return SecurityAlgorithms.HmacSha384;
                case HashAlgorithms.SHA2_512:
                    return SecurityAlgorithms.HmacSha512;
                case HashAlgorithms.SHA2_512_224:
                    return SecurityAlgorithms.HmacSha256Signature;
                case HashAlgorithms.SHA2_512_256:
                    return SecurityAlgorithms.HmacSha256Signature;
                case HashAlgorithms.SHA3_224:
                    return SecurityAlgorithms.HmacSha256Signature;
                case HashAlgorithms.SHA3_256:
                    return SecurityAlgorithms.HmacSha256Signature;
                case HashAlgorithms.SHA3_384:
                    return SecurityAlgorithms.HmacSha384Signature;
                case HashAlgorithms.SHA3_512:
                    return SecurityAlgorithms.HmacSha512Signature;
                default:
                    return SecurityAlgorithms.HmacSha256;
            }
        }

        #endregion
    }
}