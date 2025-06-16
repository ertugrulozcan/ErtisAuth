using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Extensions.Authorization.Extensions;
using ErtisAuth.WebAPI.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBePrivate.Global
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
			return TokenBase.ExtractToken(authorizationHeader, out tokenType);
		}
		
		public static string GetTokenFromHeader(this ControllerBase controller, out string tokenType)
		{
			var authorizationHeader = controller.GetAuthorizationHeader();
			return TokenBase.ExtractToken(authorizationHeader, out tokenType);
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
				var utilizerSampling = utilizerIdentity.ConvertToUtilizer();
				
				// ReSharper disable once ConvertIfStatementToSwitchStatement
				if (utilizerSampling.Type == Utilizer.UtilizerType.User)
				{
					var userService = controller.HttpContext.RequestServices.GetService<IUserService>();
					if (userService != null)
					{
						var dynamicObject = userService.GetAsync(utilizerSampling.MembershipId, utilizerSampling.Id).ConfigureAwait(false).GetAwaiter().GetResult();
						var user = dynamicObject.Deserialize<User>();
						Utilizer utilizer = user;
						utilizer.Token = utilizerSampling.Token;
						utilizer.TokenType = utilizerSampling.TokenType;

						var scopes = utilizerSampling.Scopes?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
						utilizer.Scopes = scopes is { Length: > 0 } ? scopes : null;
						
						return utilizer;
					}
				}
				else if (utilizerSampling.Type == Utilizer.UtilizerType.Application)
				{
					var applicationService = controller.HttpContext.RequestServices.GetService<IApplicationService>();
					if (applicationService != null)
					{
						Utilizer application = applicationService.Get(utilizerSampling.MembershipId, utilizerSampling.Id);
						return application;
					}
				}

				return utilizerSampling;
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

		public static NotFoundObjectResult UserTypeNotFound(this ControllerBase controller, string userTypeId)
		{
			return controller.NotFound(ErtisAuthException.UserTypeNotFound(userTypeId, "_id").Error);
		}
		
		public static BadRequestObjectResult HostRequired(this ControllerBase controller)
		{
			return controller.BadRequest(ErtisAuthException.HostRequired().Error);
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
		
		public static NotFoundObjectResult WebhookNotFound(this ControllerBase controller, string webhookId)
		{
			return controller.NotFound(ErtisAuthException.WebhookNotFound(webhookId));
		}
		
		public static NotFoundObjectResult MailHookNotFound(this ControllerBase controller, string mailHookId)
		{
			return controller.NotFound(ErtisAuthException.MailHookNotFound(mailHookId));
		}

		public static NotFoundObjectResult ProviderNotFound(this ControllerBase controller, string providerId)
		{
			return controller.NotFound(ErtisAuthException.ProviderNotFound(providerId));
		}
		
		public static NotFoundObjectResult EventNotFound(this ControllerBase controller, string eventId)
		{
			return controller.NotFound(ErtisAuthException.EventNotFound(eventId));
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
		
		public static NotFoundObjectResult ActiveTokenNotFound(this ControllerBase controller, string activeTokenId)
		{
			return controller.NotFound(ErtisAuthException.ActiveTokenNotFound(activeTokenId));
		}
		
		public static NotFoundObjectResult CodePolicyNotFound(this ControllerBase controller, string id)
		{
			return controller.NotFound(ErtisAuthException.TokenCodePolicyNotFound(id));
		}
		
		public static BadRequestObjectResult SearchKeywordRequired(this ControllerBase controller)
		{
			return controller.BadRequest(ErtisAuthException.SearchKeywordRequired().Error);
		}
		
		public static async Task<IActionResult> BulkDeleteAsync(this ControllerBase controller, IDeletableMembershipBoundedService service, string membershipId, string[] ids, CancellationToken cancellationToken = default)
		{
			var utilizer = controller.GetUtilizer();
			if (ids != null)
			{
				var isDeleted = await service.BulkDeleteAsync(utilizer, membershipId, ids, cancellationToken);
				if (isDeleted != null)
				{
					if (isDeleted.Value)
					{
						return controller.NoContent();	
					}
					else
					{
						return controller.BulkDeleteFailed(ids);
					}
				}
				else
				{
					return controller.BulkDeletePartial();
				}	
			}
			else
			{
				return controller.BadRequest();
			}
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
			return controller.BadRequest(ErtisAuthException.CommandRequired());
		}
		
		#endregion
	}
}