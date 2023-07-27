using System.Collections.Generic;
using System.Net;
using Ertis.Core.Exceptions;

namespace ErtisAuth.Core.Exceptions
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
		
		public static ErtisAuthException Unauthorized(string errorMessage)
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, errorMessage, "Unauthorized");
		}
		
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
		
		public static ErtisAuthException MembershipCouldNotDeleted(string membershipId)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"This membership is already using by some membership related resources, it's could not be deleted ({membershipId})", "MembershipCouldNotDeleted");
		}

		#endregion
		
		#region User Exceptions
		
		public static ErtisAuthException UserNotFound(string field, string parameterName)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"User not found in db by given {parameterName}: <{field}>", "UserNotFound");
		}
		
		public static ErtisAuthException UsernameOrPasswordIsWrong()
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, "Username or password is wrong", "UsernameOrPasswordIsWrong");
		}
		
		public static ErtisAuthException UserWithSameUsernameAlreadyExists(string usernameOrEmail)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The user with same username or email is already exists ({usernameOrEmail})", "UserWithSameUsernameAlreadyExists");
		}
		
		public static ErtisAuthException PasswordRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "Password is required", "PasswordRequired");
		}
		
		public static ErtisAuthException PasswordMinLengthRuleError(int minLength)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"The user password length must be a minimum of {minLength} characters.", "UsernameOrPasswordIsWrong");
		}
		
		#endregion

		#region User Type Exceptions
		
		public static ErtisAuthException UserTypeNameRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "User type name is required.", "UserTypeNameRequired");
		}
		
		public static ErtisAuthException UserTypePropertiesRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "User type properties is required.", "UserTypePropertiesRequired");
		}
		
		public static ErtisAuthException UserTypeCannotBeBothAbstractAndSealed()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "User type cannot be both abstract and sealed.", "UserTypeCannotBeBothAbstractAndSealed");
		}
		
		public static ErtisAuthException InheritedTypeNotFound(string baseUserTypeName)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"The base user type '{baseUserTypeName}' not found", "InheritedTypeNotFound");
		}
		
		public static ErtisAuthException InheritedTypeIsSealed(string baseUserTypeName)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"The base user type '{baseUserTypeName}' is flagged as sealed. It's can not be used as base type.", "InheritedTypeIsSealed");
		}
		
		public static ErtisAuthException InheritedTypeIsAbstract(string baseUserTypeName)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"The base user type '{baseUserTypeName}' has abstract modifier.", "InheritedTypeIsAbstract");
		}

		public static ErtisAuthException ReservedUserTypeName(string userTypeName)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"'{userTypeName}' is a reserved name. It's can not be used as user type name.", "ReservedUserTypeName");
		}
		
		public static ErtisAuthException DuplicateFieldWithBaseType(string fieldName)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"'{fieldName}' field is already exist in base user type.", "DuplicateFieldWithBaseType");
		}

		public static ErtisAuthException VirtualFieldTypeCanNotOverwrite(string fieldName)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"The field type cannot be overwritten on virtual fields. ('{fieldName}')", "VirtualFieldTypeCanNotOverwrite");
		}

		public static ErtisAuthException UserTypeAlreadyExists(string userTypeName)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"'{userTypeName}' is already exist.", "UserTypeAlreadyExists");
		}
		
		public static ErtisAuthException UserTypeNotFound(string field, string parameterName)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"User type not found in db by given {parameterName}: <{field}>", "UserTypeNotFound");
		}
		
		public static ErtisAuthException UserTypeRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "User type is required", "UserTypeRequired");
		}
		
		public static ErtisAuthException UserTypeImmutable()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "User type is an immutable field. It's cannot be updated.", "UserTypeImmutable");
		}
		
		public static ErtisAuthException UserTypeCanNotBeDelete()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "This user type currently using by another users, it's cannot be deleted.", "UserTypeCanNotBeDelete");
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
		
		public static ErtisAuthException RoleRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, $"Role required", "RoleRequired");
		}
		
		public static ErtisAuthException RoleNotFound(string roleIdOrName, bool passedRoleName = false)
		{
			if (passedRoleName)
			{
				return new ErtisAuthException(HttpStatusCode.NotFound, $"Role not found in db by given name: <{roleIdOrName}>", "RoleNotFound");
			}
			else
			{
				return new ErtisAuthException(HttpStatusCode.NotFound, $"Role not found in db by given _id: <{roleIdOrName}>", "RoleNotFound");	
			}
		}
		
		public static ErtisAuthException RoleWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The role with same name is already exists ({name})", "RoleWithSameNameAlreadyExists");
		}
		
		public static ErtisAuthException ReservedRoleName(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"This role name is reserved by the system ({name})", "ReservedRoleName");
		}
		
		public static ErtisAuthException UbacsConflicted(string message)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, message, "UbacsConflicted");
		}
		
		#endregion
		
		#region Provider Exceptions
		
		public static ErtisAuthException ProviderNotFound(string providerId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Provider not found in db by given _id: <{providerId}>", "ProviderNotFound");
		}
		
		public static ErtisAuthException ProviderWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The provider with same name is already exists ({name})", "ProviderWithSameNameAlreadyExists");
		}
		
		public static ErtisAuthException YouCanNotCreateCustomProvider()
		{
			return new ErtisAuthException(HttpStatusCode.Forbidden, "You can not create custom provider", "YouCanNotCreateCustomProvider");
		}
		
		public static ErtisAuthException ProviderIsDisable()
		{
			return new ErtisAuthException(HttpStatusCode.Forbidden, "Provider is disable", "ProviderIsDisable");
		}
		
		public static ErtisAuthException UntrustedProvider()
		{
			return new ErtisAuthException(HttpStatusCode.Forbidden, "Untrusted provider", "UntrustedProvider");
		}
		
		public static ErtisAuthException ProviderNotConfigured()
		{
			return new ErtisAuthException(HttpStatusCode.Forbidden, "Provider not configured", "ProviderNotConfigured");
		}
		
		public static ErtisAuthException ProviderNotConfiguredCorrectly(string message)
		{
			return new ErtisAuthException(HttpStatusCode.NotImplemented, $"Provider not configured correctly. ({message})", "ProviderNotConfiguredCorrectly");
		}
		
		public static ErtisAuthException UnsupportedProvider()
		{
			return new ErtisAuthException(HttpStatusCode.Forbidden, "Provider is not supported", "UnsupportedProvider");
		}
		
		public static ErtisAuthException ProviderNameRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "Provider name is required", "ProviderNameRequired");
		}
		
		public static ErtisAuthException UnknownProvider(string providerName)
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "Unknown provider: " + providerName, "UnknownProvider");
		}

		#endregion

		#region Webhook Exceptions

		public static ErtisAuthException WebhookNotFound(string webhookId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Webhook not found in db by given _id: <{webhookId}>", "WebhookNotFound");
		}
		
		public static ErtisAuthException WebhookWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The webhook with same name is already exists ({name})", "WebhookWithSameNameAlreadyExists");
		}

		#endregion
		
		#region MailHook Exceptions

		public static ErtisAuthException MailHookNotFound(string id)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Mail hook not found in db by given _id: <{id}>", "MailHookNotFound");
		}
		
		public static ErtisAuthException MailHookWithSameNameAlreadyExists(string name)
		{
			return new ErtisAuthException(HttpStatusCode.Conflict, $"The mail hook with same name is already exists ({name})", "MailHookWithSameNameAlreadyExists");
		}

		#endregion
		
		#region Event Exceptions

		public static ErtisAuthException EventNotFound(string eventId)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Event not found in db by given _id: <{eventId}>", "EventNotFound");
		}

		#endregion

		#region Active Token Exceptions

		public static ErtisAuthException ActiveTokenNotFound(string id)
		{
			return new ErtisAuthException(HttpStatusCode.NotFound, $"Active token not found in db by given id: <{id}>", "ActiveTokenNotFound");
		}

		#endregion

		#region Search Exceptions

		public static ErtisAuthException SearchKeywordRequired()
		{
			return new ErtisAuthException(HttpStatusCode.BadRequest, "Search keyword is required", "SearchKeywordRequired");
		}

		#endregion

		#region BulkDelete Exceptions

		public static ErtisAuthException BulkDeleteFailed(IEnumerable<string> ids)
		{
			if (ids != null)
			{
				return new ErtisAuthException(HttpStatusCode.NotFound, $"Bulk delete operation failed ({string.Join(", ", ids)})", "BulkDeleteFailed");
			}
			else
			{
				return new ErtisAuthException(HttpStatusCode.NotFound, "Bulk delete operation failed (ids: null)", "BulkDeleteFailed");
			}
		}
		
		public static ErtisAuthException BulkDeletePartial()
		{
			return new ErtisAuthException(HttpStatusCode.OK, $"Bulk delete operation was partial completed", "BulkDeletePartial");
		}

		#endregion

		#region Migration Exceptions

		public static ErtisAuthException MigrationRejected(string message)
		{
			return new ErtisAuthException(HttpStatusCode.Unauthorized, message, "MigrationRejected");
		}

		#endregion
	}
}