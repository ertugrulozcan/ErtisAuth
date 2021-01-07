using System.Collections.Generic;
using System.Net;
using Ertis.Core.Exceptions;

namespace ErtisAuth.Infrastructure.Exceptions
{
	public class ErtisAuthException : ErtisException
	{
		#region Constructors

		protected ErtisAuthException(HttpStatusCode statusCode, string message, string errorCode) : base(statusCode, message, errorCode)
		{
			
		}

		#endregion

		#region Validation Exceptions
		
		public static ValidationException ValidationError(IEnumerable<string> errors)
		{
			return new ValidationException(HttpStatusCode.BadRequest, "Some fields are not validated, invalid or missing. Check response detail.", "ModelValidationError")
			{
				Errors = errors
			};
		}
		
		public static ValidationException IdenticalDocument()
		{
			return new ValidationException(HttpStatusCode.Conflict, "There is no any difference between provided and existing document.", "IdenticalDocumentError");
		}

		#endregion
		
		#region Token Exceptions
		
		public static ErtisAuthException AuthorizationHeaderMissing()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "Authorization header missing or empty", "AuthorizationHeaderMissing");
		}
		
		public static ErtisAuthException XErtisAliasMissing(string aliasName)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"Membership id should be added in headers with '{aliasName}' key.", "XErtisAliasMissing");
		}
		
		public static ErtisAuthException InvalidToken(string message = null)
		{
			if (string.IsNullOrEmpty(message))
			{
				return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided token is invalid", "InvalidToken");
			}
			else
			{
				return new ErtisAuthException(HttpStatusCode.Unauthorized, message, "InvalidToken");	
			}
		}
		
		public static ErtisAuthException UnsupportedTokenType()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "Token type not supported. Token type must be one of Bearer or Basic", "TokenTypeNotSupported");
		}
		
		public static ErtisAuthException TokenWasRevoked()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided token was revoked", "TokenWasRevoked");
		}
		
		public static ErtisAuthException RefreshTokenWasRevoked()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided refresh token was revoked", "RefreshTokenWasRevoked");
		}
		
		public static ErtisAuthException TokenWasExpired()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided token was expired", "TokenWasExpired");
		}
		
		public static ErtisAuthException RefreshTokenWasExpired()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided refresh token was expired", "RefreshTokenWasExpired");
		}
		
		public static ErtisAuthException TokenIsNotRefreshable()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided token is not refreshable", "TokenIsNotRefreshable");
		}
		
		public static ErtisAuthException AccessDenied(string message)
		{
			return new ErtisAuthException(HttpStatusCode.Forbidden, message, "AccessDenied");
		}

		#endregion
		
		#region Membership Exceptions
		
		public static ErtisAuthException MembershipNotFound(string membershipId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Membership not found in db by given membership_id: <{membershipId}>", "MembershipNotFound");
		}
		
		public static ValidationException MalformedMembership(string membershipId, IEnumerable<string> errors)
		{
			return new ValidationException(HttpStatusCode.NotImplemented, $"Membership is not configured properly (membership_id: '{membershipId}')", "MalformedMembership")
			{
				Errors = errors
			};
		}
		
		public static ErtisAuthException MembershipAlreadyExists(string membershipId)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"Membership is already exists ({membershipId})", "MembershipAlreadyExists");
		}
		
		#endregion
		
		#region User Exceptions
		
		public static ErtisAuthException UserNotFound(string field, string parameterName)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"User not found in db by given {parameterName}: <{field}>", "UserNotFound");
		}
		
		public static ErtisAuthException UsernameOrPasswordIsWrong(string username, string password)
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Username or password is wrong", "UsernameOrPasswordIsWrong");
		}
		
		public static ErtisAuthException UserWithSameUsernameAlreadyExists(string usernameOrEmail)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The user with same username or email is already exists ({usernameOrEmail})", "UserWithSameUsernameAlreadyExists");
		}
		
		#endregion

		#region Application Exceptions
		
		public static ErtisAuthException ApplicationNotFound(string id)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Application not found in db by given id: <{id}>", "ApplicationNotFound");
		}

		public static ErtisAuthException ApplicationWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The application with same name is already exists ({name})", "ApplicationWithSameNameAlreadyExists");
		}
		
		public static ErtisAuthException ApplicationSecretMismatch()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Application secret mismatch", "ApplicationSecretMismatch");
		}

		#endregion
		
		#region Role Exceptions
		
		public static ErtisAuthException RoleNotFound(string roleId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Role not found in db by given _id: <{roleId}>", "RoleNotFound");
		}
		
		public static ErtisAuthException RoleWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The role with same name is already exists ({name})", "RoleWithSameNameAlreadyExists");
		}
		
		#endregion
		
		#region Provider Exceptions
		
		public static ErtisAuthException ProviderNotFound(string providerId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Provider not found in db by given _id: <{providerId}>", "ProviderNotFound");
		}
		
		public static ErtisAuthException ProviderWithSameSlugAlreadyExists(string slug)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The provider with same slug is already exists ({slug})", "ProviderWithSameSlugAlreadyExists");
		}
		
		#endregion

		#region Event Exceptions

		public static ErtisAuthException EventNotFound(string eventId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Event not found in db by given _id: <{eventId}>", "EventNotFound");
		}

		#endregion
	}
}