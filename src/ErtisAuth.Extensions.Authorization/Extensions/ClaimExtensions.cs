using System.Security.Claims;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ErtisAuth.Extensions.Authorization.Extensions;

public static class ClaimExtensions
{
	#region Constants
	
	public const string UtilizerClaimName = "Utilizer";
	public const string PublicClaimName = "Public";
	
	private const string IdClaimName = "utilizer_id";
	private const string TypeClaimName = "utilizer_type";
	private const string UsernameClaimName = "utilizer_username";
	private const string RoleClaimName = "role";
	private const string MembershipIdClaimName = "membership_id";
	private const string TokenClaimName = "access_token";
	private const string TokenTypeClaimName = "token_type";
	private const string ScopeClaimName = "scope";
	
	#endregion
	
	#region Methods
	
	public static Utilizer ConvertToUtilizer(this ClaimsIdentity utilizerIdentity)
	{
		var idClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == IdClaimName);
		var typeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == TypeClaimName);
		var usernameClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == UsernameClaimName);
		var roleClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == RoleClaimName);
		var membershipIdClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == MembershipIdClaimName);
		var tokenClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == TokenClaimName);
		var tokenTypeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == TokenTypeClaimName);
		var scopeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == ScopeClaimName);
		
		if (string.IsNullOrEmpty(idClaim?.Value))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an id");
		}
		
		if (string.IsNullOrEmpty(usernameClaim?.Value))
		{
			throw ErtisAuthException.InvalidUtilizer("Utilizer username is null or empty in claims");
		}
		
		if (string.IsNullOrEmpty(roleClaim?.Value))
		{
			throw ErtisAuthException.InvalidUtilizer("Utilizer role is null or empty in claims");
		}
		
		if (string.IsNullOrEmpty(tokenClaim?.Value))
		{
			throw ErtisAuthException.InvalidUtilizer("Utilizer token is null or empty in claims");
		}
		
		if (string.IsNullOrEmpty(membershipIdClaim?.Value))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an membership id");
		}
		
		var tokenType = tokenTypeClaim?.Value != null ? TokenTypeExtensions.TryParseTokenType(tokenTypeClaim.Value, out var tokenType_) ? tokenType_ : SupportedTokenTypes.None : SupportedTokenTypes.None;
		var scopes = scopeClaim?.Value.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
		
		return new Utilizer
		{
			Id = idClaim.Value,
			Type = typeClaim?.Value != null ? Utilizer.ParseType(typeClaim.Value) : Utilizer.UtilizerType.None,
			Username = usernameClaim.Value,
			Role = roleClaim.Value,
			MembershipId = membershipIdClaim.Value,
			Token = tokenClaim.Value,
			TokenType = tokenType,
			Scopes = scopes is { Length: > 0 } ? scopes : null
		};
	}
	
	public static Utilizer ConvertToUtilizer(this JsonWebToken token)
	{
		var idClaim = token.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
		var membershipIdClaim = token.Claims.FirstOrDefault(x => x.Type == "prn")?.Value;
		var usernameClaim = token.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value;
		
		if (string.IsNullOrEmpty(idClaim))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an id");
		}
		
		if (string.IsNullOrEmpty(usernameClaim))
		{
			throw ErtisAuthException.InvalidUtilizer("Utilizer username is null or empty in claims");
		}
		
		if (string.IsNullOrEmpty(membershipIdClaim))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an membership id");
		}
		
		return new Utilizer
		{
			Id = idClaim,
			Type = Utilizer.UtilizerType.User,
			Username = usernameClaim,
			Role = null,
			MembershipId = membershipIdClaim,
			Token = token.EncodedToken,
			TokenType = SupportedTokenTypes.Bearer
		};
	}
	
	public static ClaimsIdentity ToClaimsIdentity(this Utilizer utilizer)
	{
		var claims = new List<Claim>
		{
			new (type: TypeClaimName, utilizer.Type.ToString())
		};
		
		if (!string.IsNullOrEmpty(utilizer.Id))
		{
			claims.Add(new Claim(type: IdClaimName, utilizer.Id));
		}
		
		if (!string.IsNullOrEmpty(utilizer.Username))
		{
			claims.Add(new Claim(type: UsernameClaimName, utilizer.Username));
		}
		
		if (!string.IsNullOrEmpty(utilizer.Role))
		{
			claims.Add(new Claim(type: RoleClaimName, utilizer.Role));
		}
		
		if (!string.IsNullOrEmpty(utilizer.MembershipId))
		{
			claims.Add(new Claim(type: MembershipIdClaimName, utilizer.MembershipId));
		}
		
		if (!string.IsNullOrEmpty(utilizer.Token))
		{
			claims.Add(new Claim(type: TokenClaimName, utilizer.Token));
		}
		
		if (!string.IsNullOrEmpty(utilizer.Token))
		{
			claims.Add(new Claim(type: TokenTypeClaimName, utilizer.TokenType.ToString()));
		}
		
		if (utilizer.Scopes is { Length: > 0 })
		{
			claims.Add(new Claim(type: ScopeClaimName, string.Join(" ", utilizer.Scopes.Where(x => !string.IsNullOrWhiteSpace(x)))));
		}
		
		return new ClaimsIdentity(claims, null, UtilizerClaimName, utilizer.Role);
	}
	
	#endregion
}