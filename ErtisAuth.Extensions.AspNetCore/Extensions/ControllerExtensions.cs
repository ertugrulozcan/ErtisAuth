using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Constants;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Extensions.AspNetCore.Extensions
{
	public static class ControllerExtensions
	{
		#region Methods

		public static string GetAuthorizationHeader(this HttpRequest request)
		{
			if (request.Headers.ContainsKey(Headers.AUTHORIZATION))
			{
				return request.Headers[Headers.AUTHORIZATION];
			}

			return null;
		}
		
		public static string GetAuthorizationHeader(this ControllerBase controller)
		{
			if (controller.Request.Headers.ContainsKey(Headers.AUTHORIZATION))
			{
				return controller.Request.Headers[Headers.AUTHORIZATION];
			}

			return null;
		}
		
		public static string GetTokenFromHeader(this HttpRequest request, out string tokenType)
		{
			var authorizationHeader = request.GetAuthorizationHeader();
			return TokenHelper.ExtractToken(authorizationHeader, out tokenType);
		}
		
		public static string GetTokenFromHeader(this ControllerBase controller, out string tokenType)
		{
			var authorizationHeader = controller.GetAuthorizationHeader();
			return TokenHelper.ExtractToken(authorizationHeader, out tokenType);
		}

		public static Utilizer GetUtilizer(this ControllerBase controller)
		{
			var claimUser = controller.User;
			var utilizerIdentity = claimUser.Identities.FirstOrDefault(x => x.NameClaimType == "Utilizer");
			if (utilizerIdentity != null)
			{
				var idClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerIdClaimName);
				var usernameClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerUsernameClaimName);
				var typeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTypeClaimName);
				var roleClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerRoleClaimName);
				var tokenClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTokenClaimName);
				var tokenTypeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTokenTypeClaimName);

				TokenTypeExtensions.TryParseTokenType(tokenTypeClaim?.Value, out var tokenType);
				
				return new Utilizer
				{
					Id = idClaim?.Value,
					Username = usernameClaim?.Value,
					Type = Utilizer.ParseType(typeClaim?.Value),
					Role = roleClaim?.Value,
					Token = tokenClaim?.Value,
					TokenType = tokenType
				};
			}

			return new Utilizer();
		}
		
		public static BadRequestObjectResult AuthorizationHeaderMissing(this ControllerBase controller)
		{
			return controller.BadRequest(ErtisAuthException.AuthorizationHeaderMissing().Error);
		}

		public static UnauthorizedObjectResult InvalidToken(this ControllerBase controller)
		{
			return controller.Unauthorized(ErtisAuthException.InvalidToken().Error);
		}

		#endregion
	}
}