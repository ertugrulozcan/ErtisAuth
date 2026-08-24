using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Extensions.Authorization.Extensions;

public static class ClaimExtensions
{
	#region Methods
	
	public static Utilizer ConvertToUtilizer(this ClaimsIdentity utilizerIdentity)
	{
		var idClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerIdClaimName);
		var typeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTypeClaimName);
		var usernameClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerUsernameClaimName);
		var roleClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerRoleClaimName);
		var membershipIdClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.MembershipIdClaimName);
		var tokenClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTokenClaimName);
		var tokenTypeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTokenTypeClaimName);
		var scopeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.ScopeClaimName);
		
		if (string.IsNullOrEmpty(idClaim?.Value))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an id");
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
			Username = usernameClaim?.Value,
			Role = roleClaim?.Value,
			MembershipId = membershipIdClaim.Value,
			Token = tokenClaim?.Value,
			TokenType = tokenType,
			Scopes = scopes is { Length: > 0 } ? scopes : null
		};
	}
	
	public static Utilizer ConvertToUtilizer(this JwtSecurityToken token)
	{
		var idClaim = token.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
		var membershipIdClaim = token.Claims.FirstOrDefault(x => x.Type == "prn")?.Value;
		
		if (string.IsNullOrEmpty(idClaim))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an id");
		}
		
		if (string.IsNullOrEmpty(membershipIdClaim))
		{
			throw ErtisAuthException.InvalidUtilizer("The utilizer does not have an membership id");
		}
		
		return new Utilizer
		{
			Id = idClaim,
			Type = Utilizer.UtilizerType.User,
			Username = token.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value,
			Role = null,
			MembershipId = membershipIdClaim,
			Token = token.RawData,
			TokenType = SupportedTokenTypes.Bearer
		};
	}
	
	#endregion
}