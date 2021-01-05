using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Helpers;
using ErtisAuth.WebAPI.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Extensions
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
		
		public static string GetXErtisAlias(this ControllerBase controller)
		{
			if (controller.Request.Headers.ContainsKey(Headers.X_ERTIS_ALIAS))
			{
				return controller.Request.Headers[Headers.X_ERTIS_ALIAS];
			}

			return null;
		}

		public static Utilizer GetUtilizer(this ControllerBase controller)
		{
			var claimUser = controller.User;
			var utilizerIdentity = claimUser.Identities.FirstOrDefault(x => x.NameClaimType == "Utilizer");
			if (utilizerIdentity != null)
			{
				var idClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerIdClaimName);
				var typeClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerTypeClaimName);
				var roleClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.UtilizerRoleClaimName);
				var membershipIdClaim = utilizerIdentity.Claims.FirstOrDefault(x => x.Type == Utilizer.MembershipIdClaimName);

				return new Utilizer
				{
					Id = idClaim?.Value,
					Type = typeClaim?.Value,
					Role = roleClaim?.Value,
					MembershipId = membershipIdClaim?.Value
				};
			}

			return new Utilizer();
		}
		
		public static BadRequestObjectResult AuthorizationHeaderMissing(this ControllerBase controller)
		{
			return controller.BadRequest(ErtisAuthException.AuthorizationHeaderMissing().Error);
		}
		
		public static BadRequestObjectResult XErtisAliasMissing(this ControllerBase controller)
		{
			return controller.BadRequest(ErtisAuthException.XErtisAliasMissing(Headers.X_ERTIS_ALIAS).Error);
		}

		public static NotFoundObjectResult UserNotFound(this ControllerBase controller, string userId)
		{
			return controller.NotFound(ErtisAuthException.UserNotFound(userId, "_id").Error);
		}
		
		public static NotFoundObjectResult ApplicationNotFound(this ControllerBase controller, string applicationId)
		{
			return controller.NotFound(ErtisAuthException.ApplicationNotFound(applicationId));
		}
		
		public static NotFoundObjectResult MembershipNotFound(this ControllerBase controller, string membershipId)
		{
			return controller.NotFound(ErtisAuthException.MembershipNotFound(membershipId));
		}
		
		public static NotFoundObjectResult RoleNotFound(this ControllerBase controller, string roleId)
		{
			return controller.NotFound(ErtisAuthException.RoleNotFound(roleId));
		}
		
		public static NotFoundObjectResult EventNotFound(this ControllerBase controller, string eventId)
		{
			return controller.NotFound(ErtisAuthException.EventNotFound(eventId));
		}

		public static UnauthorizedObjectResult UsernameOrPasswordIsWrong(this ControllerBase controller, string username, string password)
		{
			return controller.Unauthorized(ErtisAuthException.UsernameOrPasswordIsWrong(username, password).Error);
		}
		
		public static UnauthorizedObjectResult InvalidToken(this ControllerBase controller)
		{
			return controller.Unauthorized(ErtisAuthException.InvalidToken().Error);
		}

		#endregion
	}
}