using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ErtisAuth.Core.Models.Cryptography;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Identity.Jwt.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Identity.Jwt.Services;

public class JwtService : IJwtService
{
    #region Methods
    
    public string GenerateToken(TokenClaims tokenClaims, HashAlgorithms hashAlgorithm, Encoding encoding, TimeSpan? expiresIn = null)
    {
        return this.GenerateToken(
            hashAlgorithm,
            encoding,
            DateTime.Now, 
            tokenClaims.SecretKey,
            tokenClaims.Issuer,
            tokenClaims.Audience,
            expiresIn ?? tokenClaims.ExpiresIn,
            tokenClaims.Subject,
            tokenClaims.TokenId,
            tokenClaims.Principal,
            tokenClaims.FirstName,
            tokenClaims.LastName,
			tokenClaims.Username,
            tokenClaims.EmailAddress,
            tokenClaims.Scope,
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
            tokenClaims.Scope,
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
        string? subject = null,
        string? tokenId = null,
        string? principal = null,
        string? firstName = null,
        string? lastName = null,
        string? username = null,
        string? email = null,
        string? scope = null,
        IDictionary<string, object>? additionalClaims = null)
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
            new(JwtRegisteredClaimNames.Azp, audience),
            new(JwtRegisteredClaimNames.Iat, timestamp.ToString())
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
        
        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim("scope", scope));
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
    
    public bool ValidateToken(string token, TokenClaims claims, SymmetricSecurityKey secretKey, out SecurityToken? validatedToken)
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
                RequireSignedTokens = true
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
        return (JwtSecurityToken) handler.ReadToken(token);
    }
    
    public bool TryDecodeToken(string token, out JwtSecurityToken? securityToken)
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
        return hashAlgorithm switch
        {
            HashAlgorithms.MD5 => SecurityAlgorithms.Ripemd160Digest,
            HashAlgorithms.SHA0 => SecurityAlgorithms.Sha256,
            HashAlgorithms.SHA1 => SecurityAlgorithms.Sha256,
            HashAlgorithms.SHA2_224 => SecurityAlgorithms.HmacSha256,
            HashAlgorithms.SHA2_256 => SecurityAlgorithms.HmacSha256,
            HashAlgorithms.SHA2_384 => SecurityAlgorithms.HmacSha384,
            HashAlgorithms.SHA2_512 => SecurityAlgorithms.HmacSha512,
            HashAlgorithms.SHA2_512_224 => SecurityAlgorithms.HmacSha256Signature,
            HashAlgorithms.SHA2_512_256 => SecurityAlgorithms.HmacSha256Signature,
            HashAlgorithms.SHA3_224 => SecurityAlgorithms.HmacSha256Signature,
            HashAlgorithms.SHA3_256 => SecurityAlgorithms.HmacSha256Signature,
            HashAlgorithms.SHA3_384 => SecurityAlgorithms.HmacSha384Signature,
            HashAlgorithms.SHA3_512 => SecurityAlgorithms.HmacSha512Signature,
            _ => SecurityAlgorithms.HmacSha256
        };
    }
    
    #endregion
}