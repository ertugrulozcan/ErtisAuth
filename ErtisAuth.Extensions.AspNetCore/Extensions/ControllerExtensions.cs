using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.Extensions.AspNetCore.Extensions;

public static class ControllerExtensions
{
	#region Authorization Methods
	
	public static string? GetAuthorizationHeader(this ControllerBase controller)
	{
		if (controller.Request.Headers.TryGetValue("Authorization", out var value))
		{
			return value;
		}
		
		return null;
	}
	
	public static string? GetAuthorizationHeader(this HttpRequest request)
	{
		if (request.Headers.TryGetValue("Authorization", out var value))
		{
			return value;
		}
		
		return null;
	}
	
	public static BadRequestObjectResult AuthorizationHeaderMissing(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.AuthorizationHeaderMissing().Error);
	}
	
	#endregion
	
	#region Token Methods
	
	public static string? GetTokenFromHeader(this ControllerBase controller, out string? tokenType)
	{
		var authorizationHeader = controller.GetAuthorizationHeader();
		return TokenBase.ExtractToken(authorizationHeader, out tokenType);
	}
	
	public static string? GetTokenFromHeader(this HttpRequest request, out string? tokenType)
	{
		var authorizationHeader = request.GetAuthorizationHeader();
		return TokenBase.ExtractToken(authorizationHeader, out tokenType);
	}
	
	public static UnauthorizedObjectResult InvalidCredentials(this ControllerBase controller)
	{
		return controller.Unauthorized(ErtisAuthException.InvalidCredentials().Error);
	}
	
	public static UnauthorizedObjectResult InvalidToken(this ControllerBase controller)
	{
		return controller.Unauthorized(ErtisAuthException.InvalidToken().Error);
	}
	
	public static BadRequestObjectResult UnsupportedTokenType(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.UnsupportedTokenType().Error);
	}
	
	public static BadRequestObjectResult BearerTokenRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.BearerTokenRequired().Error);
	}
	
	public static BadRequestObjectResult PasswordRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.PasswordRequired().Error);
	}
	
	public static BadRequestObjectResult EmailAddressRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.EmailAddressRequired().Error);
	}
	
	public static BadRequestObjectResult ResetTokenRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.ResetTokenRequired().Error);
	}
	
	public static NotFoundObjectResult ActiveTokenNotFound(this ControllerBase controller, string activeTokenId)
	{
		return controller.NotFound(ErtisAuthException.ActiveTokenNotFound(activeTokenId).Error);
	}
	
	public static BadRequestObjectResult UnknownPlatform(this ControllerBase controller, string platformName)
	{
		return controller.BadRequest(ErtisAuthException.UnknownPlatform(platformName).Error);
	}
	
	public static NotFoundObjectResult CodePolicyNotFound(this ControllerBase controller, string id)
	{
		return controller.NotFound(ErtisAuthException.TokenCodePolicyNotFound(id).Error);
	}
	
	#endregion
	
	#region Membership Methods
	
	public static string? GetMembershipId(this ControllerBase controller)
	{
		var headers = new[]
		{
			"Membership",
			"MembershipId",
			"X-Ertis-Alias"
		};
		
		foreach (var header in headers)
		{
			if (controller.Request.Headers.ContainsKey(header))
			{
				return controller.Request.Headers[header].ToString();
			}
		}
		
		return null;
	}
	
	public static BadRequestObjectResult MembershipIdRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.MembershipIdRequired().Error);
	}
	
	public static NotFoundObjectResult MembershipNotFound(this ControllerBase controller, string membershipId)
	{
		return controller.NotFound(ErtisAuthException.MembershipNotFound(membershipId).Error);
	}
	
	#endregion
	
	#region User Methods
	
	public static NotFoundObjectResult UserNotFound(this ControllerBase controller, string userId)
	{
		return controller.NotFound(ErtisAuthException.UserNotFound(userId, "_id").Error);
	}
	
	public static BadRequestObjectResult HostRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.HostRequired().Error);
	}
	
	#endregion
	
	#region User Type Methods
	
	public static NotFoundObjectResult UserTypeNotFound(this ControllerBase controller, string userTypeId)
	{
		return controller.NotFound(ErtisAuthException.UserTypeNotFound(userTypeId, "_id").Error);
	}
	
	#endregion
	
	#region Application Methods
	
	public static NotFoundObjectResult ApplicationNotFound(this ControllerBase controller, string applicationId)
	{
		return controller.NotFound(ErtisAuthException.ApplicationNotFound(applicationId).Error);
	}
	
	#endregion
	
	#region Role Methods
	
	public static NotFoundObjectResult RoleNotFound(this ControllerBase controller, string roleId)
	{
		return controller.NotFound(ErtisAuthException.RoleNotFound(roleId).Error);
	}
	
	#endregion
	
	#region Webhook Methods
	
	public static NotFoundObjectResult WebhookNotFound(this ControllerBase controller, string webhookId)
	{
		return controller.NotFound(ErtisAuthException.WebhookNotFound(webhookId).Error);
	}
	
	#endregion
	
	#region Mailhook Methods
	
	public static NotFoundObjectResult MailHookNotFound(this ControllerBase controller, string mailHookId)
	{
		return controller.NotFound(ErtisAuthException.MailHookNotFound(mailHookId).Error);
	}
	
	#endregion
	
	#region Provider Methods
	
	public static NotFoundObjectResult ProviderNotFound(this ControllerBase controller, string providerId)
	{
		return controller.NotFound(ErtisAuthException.ProviderNotFound(providerId).Error);
	}
	
	#endregion
	
	#region Event Methods
	
	public static NotFoundObjectResult EventNotFound(this ControllerBase controller, string eventId)
	{
		return controller.NotFound(ErtisAuthException.EventNotFound(eventId).Error);
	}
	
	#endregion
	
	#region Common Methods
	
	public static BadRequestObjectResult SearchKeywordRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.SearchKeywordRequired().Error);
	}
	
	public static NotFoundObjectResult BulkDeleteFailed(this ControllerBase controller, IEnumerable<string> ids)
	{
		return controller.NotFound(ErtisAuthException.BulkDeleteFailed(ids).Error);
	}
	
	public static OkObjectResult BulkDeletePartial(this ControllerBase controller)
	{
		return controller.Ok(ErtisAuthException.BulkDeletePartial().Error);
	}
	
	public static BadRequestObjectResult CommandRequired(this ControllerBase controller)
	{
		return controller.BadRequest(ErtisAuthException.CommandRequired().Error);
	}
	
	#endregion
}