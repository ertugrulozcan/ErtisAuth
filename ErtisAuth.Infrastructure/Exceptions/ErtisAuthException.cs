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
		
		#endregion
		
		#region Token Exceptions
		
		public static ErtisAuthException InvalidToken()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Provided token is invalid", "InvalidToken");
		}
		
		public static ErtisAuthException TokenTypeNotSupported()
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

		#region UserType Exceptions
		
		public static ErtisAuthException UserTypeNotFound(string id)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"User type not found in db by given _id: <{id}>", "UserTypeNotFound");
		}
		
		public static ErtisAuthException UserTypeWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The user type with same name is already exists ({name})", "UserTypeWithSameNameAlreadyExists");
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
	}
}