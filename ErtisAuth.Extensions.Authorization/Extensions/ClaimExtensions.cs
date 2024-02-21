using System.Linq;
using System.Security.Claims;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Extensions.Authorization.Extensions
{
	public static class ClaimExtensions
	{
		#region Methods

		public static Utilizer ConvertToUtilizer(this ClaimsIdentity utilizerIdentity)
		{
			var idClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerIdClaimName);
			var typeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTypeClaimName);
			var usernameClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerUsernameClaimName);
			var emailClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerEmailAddressClaimName);
			var roleClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerRoleClaimName);
			var membershipIdClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.MembershipIdClaimName);
			var tokenClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTokenClaimName);
			var tokenTypeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTokenTypeClaimName);

			TokenTypeExtensions.TryParseTokenType(tokenTypeClaim?.Value, out var tokenType);
				
			return new Utilizer
			{
				Id = idClaim?.Value,
				Type = Utilizer.ParseType(typeClaim?.Value),
				Username = usernameClaim?.Value,
				EmailAddress = emailClaim?.Value,
				Role = roleClaim?.Value,
				MembershipId = membershipIdClaim?.Value,
				Token = tokenClaim?.Value,
				TokenType = tokenType
			};
		}

		#endregion
	}
}