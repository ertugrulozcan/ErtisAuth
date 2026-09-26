using System.Text;
using System.Security.Claims;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
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
    
    public string GenerateToken(TokenClaims tokenClaims, DateTime? generationTime = null, TimeSpan? expiresIn = null, Encoding? encoding = null)
    {
        var generatedAt = generationTime ?? DateTime.UtcNow;
        var expireTime = generatedAt.Add(expiresIn ?? tokenClaims.ExpiresIn);
        var timestamp = new DateTimeOffset(generatedAt).ToUnixTimeSeconds();
        var securityKey = new SymmetricSecurityKey((encoding ?? Encoding.UTF8).GetBytes(tokenClaims.SecretKey));
        var credentials = new SigningCredentials(securityKey, SigningHashAlgorithm);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Azp, tokenClaims.Audience),
            new(JwtRegisteredClaimNames.Iat, timestamp.ToString())
        };
        
        if (!string.IsNullOrEmpty(tokenClaims.Subject))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, tokenClaims.Subject));
        }
        
        if (!string.IsNullOrEmpty(tokenClaims.TokenId))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenClaims.TokenId));
        }
        
        if (!string.IsNullOrEmpty(tokenClaims.Principal))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Prn, tokenClaims.Principal));
        }
        
        if (!string.IsNullOrEmpty(tokenClaims.FirstName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, tokenClaims.FirstName));
        }
        
        if (!string.IsNullOrEmpty(tokenClaims.LastName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, tokenClaims.LastName));
        }
        
        if (!string.IsNullOrEmpty(tokenClaims.Username))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, tokenClaims.Username));
        }
        
		if (!string.IsNullOrEmpty(tokenClaims.EmailAddress))
		{
			claims.Add(new Claim(JwtRegisteredClaimNames.Email, tokenClaims.EmailAddress));
		}
        
        if (!string.IsNullOrEmpty(tokenClaims.Scope))
        {
            claims.Add(new Claim("scope", tokenClaims.Scope));
        }
        
        foreach (var additionalClaim in tokenClaims.AdditionalClaims)
        {
            if (!claims.Exists(x => x.Type == additionalClaim.Key) && additionalClaim.Value != null)
            {
                claims.Add(new Claim(additionalClaim.Key, additionalClaim.Value.ToString() ?? string.Empty));
            }
        }
        
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = tokenClaims.Issuer,
            Audience = tokenClaims.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expireTime,
            SigningCredentials = credentials
        };
        
        return this._tokenHandler.CreateToken(descriptor);
    }
    
    public async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenClaims claims, SymmetricSecurityKey secretKey)
    {
        return await this.ValidateTokenAsync(token, claims.Issuer, claims.Audience, secretKey);
    }
	
	public async Task<TokenValidationResult> ValidateTokenAsync(string token, Membership membership)
	{
		var secretKey = new SymmetricSecurityKey(membership.GetEncoding().GetBytes(membership.SecretKey));
		return await this.ValidateTokenAsync(token, membership.Name, membership.Slug, secretKey);
	}
	
    private async Task<TokenValidationResult> ValidateTokenAsync(string token, string issuer, string audience, SymmetricSecurityKey secretKey)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
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