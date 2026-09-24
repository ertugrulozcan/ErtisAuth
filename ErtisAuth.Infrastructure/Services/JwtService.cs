using System.Text;
using System.Security.Claims;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Abstractions.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Infrastructure.Services;

public class JwtService : IJwtService
{
    #region Constants
    
    private const string SigningHashAlgorithm = SecurityAlgorithms.HmacSha256;
    
    #endregion
    
    #region Services
    
    private readonly JsonWebTokenHandler _tokenHandler;
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    public JwtService()
    {
        this._tokenHandler = new JsonWebTokenHandler();
    }
    
    #endregion
    
    #region Methods
    
    public string GenerateToken(TokenClaims tokenClaims, Encoding encoding, TimeSpan? expiresIn = null)
    {
        return this.GenerateToken(
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
    
    public string GenerateToken(TokenClaims tokenClaims, DateTime tokenGenerationTime, Encoding encoding)
    {
        return this.GenerateToken(
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
        var credentials = new SigningCredentials(securityKey, SigningHashAlgorithm);
        
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
        
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expireTime,
            SigningCredentials = credentials
        };
        
        return this._tokenHandler.CreateToken(descriptor);
    }
    
    public async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenClaims claims, SymmetricSecurityKey secretKey)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = claims.Issuer,
            ValidAudience = claims.Audience,
            IssuerSigningKey = secretKey,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [
                SigningHashAlgorithm
            ]
        };
        
        return await this._tokenHandler.ValidateTokenAsync(token, validationParameters);
    }
    
    public JsonWebToken DecodeToken(string token)
    {
        return this._tokenHandler.ReadJsonWebToken(token);
    }
    
    public bool TryDecodeToken(string token, out JsonWebToken? securityToken)
    {
        try
        {
            if (!this._tokenHandler.CanReadToken(token))
            {
                securityToken = null;
                return false;
            }
            
            securityToken = this.DecodeToken(token);
            return true;
        }
        catch
        {
            securityToken = null;
            return false;
        }
    }
    
    #endregion
}