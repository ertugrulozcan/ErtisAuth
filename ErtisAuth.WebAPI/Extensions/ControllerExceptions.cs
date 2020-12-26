using System.Net;
using Ertis.Core.Models.Response;
using ErtisAuth.Infrastructure.Exceptions;
using ErtisAuth.Infrastructure.Helpers;
using ErtisAuth.WebAPI.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Extensions
{
	public static class ControllerExceptions
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
		
		public static BadRequestObjectResult AuthorizationHeaderMissing(this ControllerBase controller)
		{
			return controller.BadRequest(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.BadRequest,
				Message = $"Authorization header missing or empty",
				ErrorCode = "AuthorizationHeaderMissing"
			});
		}
		
		public static string GetXErtisAlias(this ControllerBase controller)
		{
			if (controller.Request.Headers.ContainsKey(Headers.X_ERTIS_ALIAS))
			{
				return controller.Request.Headers[Headers.X_ERTIS_ALIAS];
			}

			return null;
		}
		
		public static BadRequestObjectResult XErtisAliasMissing(this ControllerBase controller)
		{
			return controller.BadRequest(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.BadRequest,
				Message = $"Membership id should be added in headers with '{Headers.X_ERTIS_ALIAS}' key.",
				ErrorCode = "XErtisAliasRequired"
			});
		}

		public static NotFoundObjectResult UserNotFound(this ControllerBase controller, string userId)
		{
			return controller.NotFound(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.NotFound,
				Message = $"User not found in db by given _id: <{userId}>",
				ErrorCode = "UserNotFound"
			});
		}
		
		public static NotFoundObjectResult MembershipNotFound(this ControllerBase controller, string membershipId)
		{
			return controller.NotFound(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.NotFound,
				Message = $"Membership not found in db by given _id: <{membershipId}>",
				ErrorCode = "MembershipNotFound"
			});
		}
		
		public static NotFoundObjectResult RoleNotFound(this ControllerBase controller, string roleId)
		{
			return controller.NotFound(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.NotFound,
				Message = $"Role not found in db by given _id: <{roleId}>",
				ErrorCode = "RoleNotFound"
			});
		}
		
		public static NotFoundObjectResult EventNotFound(this ControllerBase controller, string eventId)
		{
			return controller.NotFound(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.NotFound,
				Message = $"Event not found in db by given _id: <{eventId}>",
				ErrorCode = "EventNotFound"
			});
		}

		public static UnauthorizedObjectResult Unauthorized(this ControllerBase controller, string username, string password)
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