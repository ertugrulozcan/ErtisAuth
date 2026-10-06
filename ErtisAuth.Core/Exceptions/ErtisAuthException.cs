using System.Net;
using Ertis.Core.Exceptions;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace ErtisAuth.Core.Exceptions;

public class ErtisAuthException : ErtisException
{
	#region Constructors
	
	// ReSharper disable once MemberCanBePrivate.Global
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
	
	public static ErtisAuthException Synthetic(HttpStatusCode httpStatusCode, string errorMessage, string errorCode)
	{
		return new ErtisAuthException(httpStatusCode, errorMessage, errorCode);
	}
	
	public static DuplicateKeyException DuplicateKeyError(string message, string? indexName = null)
	{
		return new DuplicateKeyException(HttpStatusCode.Conflict, $"Some fields has unique index. ({message})", "DuplicateKeyError")
		{
			IndexName = indexName
		};
	}
	
	#endregion
	
	#region Token Exceptions
	
	public static ErtisAuthException Unauthorized(string errorMessage)
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, errorMessage, "Unauthorized");
	}
	
	public static ErtisAuthException AuthorizationHeaderMissing()
	{
		// No credentials: 401 (RFC 9110 §15.5.2, RFC 6750 §3.1)
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "Authorization header missing or empty", "AuthorizationHeaderMissing");
	}
	
	public static ErtisAuthException InvalidToken(string? message = null)
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
	
	public static ErtisAuthException InvalidCredentialsOrMissingToken()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Invalid credentials or missing token", "InvalidCredentialsOrMissingToken");
	}
	
	public static ErtisAuthException UnsupportedTokenType()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Token type not supported. Token type must be one of Bearer or Basic", "TokenTypeNotSupported");
	}
	
	public static ErtisAuthException BearerTokenRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Token type must be Bearer for this action", "BearerTokenRequired");
	}
	
	public static ErtisAuthException RefreshTokenRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Refresh token required", "RefreshTokenRequired");
	}
	
	public static ErtisAuthException InvalidScope(string scope)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"Invalid scope ({scope})", "InvalidScope");
	}
	
	public static ErtisAuthException ScopeRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Scope required", "ScopeRequired");
	}
	
	public static ErtisAuthException UserHasNoPermissionForThisScope(string scope)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"User has no permission for this scope ({scope})", "UserHasNoPermissionForThisScope");
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
	
	public static ErtisAuthException TokenIsNotRefreshable(string? message = null)
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, message ?? "Provided token is not refreshable", "TokenIsNotRefreshable");
	}
	
	public static ErtisAuthException AccessDenied(string message)
	{
		return new ErtisAuthException(HttpStatusCode.Forbidden, message, "AccessDenied");
	}
	
	public static ErtisAuthException TokenCodePolicyNotFound(string? slug = null)
	{
		return string.IsNullOrEmpty(slug) 
			? new ErtisAuthException(HttpStatusCode.NotFound, "Any code policy not found in this membership", "TokenCodePolicyNotFound") 
			: new ErtisAuthException(HttpStatusCode.NotFound, $"Code policy not found in db by given slug: <{slug}>", "TokenCodePolicyNotFound");
	}
	
	public static ErtisAuthException TokenCodePolicyAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The token code policy is already exists with same slug ({slug})", "TokenCodePolicyAlreadyExists");
	}
	
	public static ErtisAuthException TokenCodeNotFound()
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, "Token code not found", "TokenCodeNotFound");
	}
	
	public static ErtisAuthException TokenCodeExpired()
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "Token code was expired", "TokenCodeExpired");
	}
	
	public static ErtisAuthException UnauthorizedTokenCode()
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "Token code unauthorized yet", "UnauthorizedTokenCode");
	}
	
	public static ErtisAuthException TokenCodeAlreadyAuthorized()
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, "Token code was already authorized", "TokenCodeAlreadyAuthorized");
	}

	public static ErtisAuthException TokenCodeDenied()
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "Token code was denied", "TokenCodeDenied");
	}

	public static ErtisAuthException TokenCodeSlowDown(int interval)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"Token code is polled too often, wait at least {interval} seconds between the polls", "TokenCodeSlowDown");
	}

	public static ErtisAuthException TokenCodeCouldNotBeGenerated()
	{
		return new ErtisAuthException(HttpStatusCode.ServiceUnavailable, "A unique token code could not be generated, please try again", "TokenCodeCouldNotBeGenerated");
	}
	
	public static ErtisAuthException TokenCodePolicyInUse(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The token code policy '{slug}' is used by the membership, it can't be deleted", "TokenCodePolicyInUse");
	}
	
	public static ErtisAuthException InvalidUtilizer(string message)
	{
		return new ErtisAuthException(HttpStatusCode.NotImplemented, message, "InvalidUtilizer");
	}
	
	public static ErtisAuthException ResetTokenRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Rest password token is required", "ResetTokenRequired");
	}
	
	#endregion
	
	#region Rbac & Ubac Exceptions
	
	public static ErtisAuthException InvalidRbac(string? message = null)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, string.IsNullOrEmpty(message) ? "Invalid rbac expression" : $"Invalid rbac expression ({message})", "InvalidRbac");
	}
	
	public static ErtisAuthException InvalidUbac(string? message = null)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, string.IsNullOrEmpty(message) ? "Invalid ubac expression" : $"Invalid ubac expression ({message})", "InvalidUbac");
	}
	
	public static ErtisAuthException RbacsConflicted(string message)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, message, "RbacsConflicted");
	}
	
	public static ErtisAuthException UbacsConflicted(string message)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, message, "UbacsConflicted");
	}
	
	#endregion
	
	#region Membership Exceptions
	
	public static ErtisAuthException MembershipNotFound(string membershipId)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"Membership not found in db by given membership_id: <{membershipId}>", "MembershipNotFound");
	}
	
	public static ErtisAuthException MembershipIdRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Membership id required", "MembershipIdRequired");
	}
	
	public static ErtisAuthException MembershipAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"Membership is already exists ({slug})", "MembershipAlreadyExists");
	}
	
	public static ErtisAuthException MembershipCouldNotDeleted(string reason)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, reason, "MembershipCouldNotDeleted");
	}
	
	public static ErtisAuthException InvalidQuery(string message)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, message, "InvalidQuery");
	}
	
	public static ErtisAuthException UnsupportedAggregationStage(string stage)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"The aggregation stage is not supported ({stage})", "UnsupportedAggregationStage");
	}
	
	public static ErtisAuthException MembershipHashAlgorithmInvalid(string membershipId, string? hashAlgorithm)
	{
		return new ErtisAuthException(HttpStatusCode.InternalServerError, $"The membership has no valid hash algorithm configured ({membershipId}: '{hashAlgorithm}')", "MembershipHashAlgorithmInvalid");
	}
	
	public static ErtisAuthException HashAlgorithmRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "The hash algorithm is required", "HashAlgorithmRequired");
	}
	
	public static ErtisAuthException UnsupportedHashAlgorithm(string hashAlgorithm)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"Unsupported hash algorithm ({hashAlgorithm})", "UnsupportedHashAlgorithm");
	}
	
	public static ErtisAuthException UnsupportedEncoding(string encoding)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"Unsupported encoding ({encoding})", "UnsupportedEncoding");
	}
	
	public static ErtisAuthException UnsupportedLanguage(string locale)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"Unsupported locale ({locale})", "UnsupportedLanguage");
	}
	
	public static ErtisAuthException UserAlreadyActive()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "User already active", "UserAlreadyActive");
	}
	
	public static ErtisAuthException UserAlreadyInactive()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "User already inactive", "UserAlreadyInactive");
	}
	
	#endregion
	
	#region User Exceptions
	
	public static ErtisAuthException UserNotFound(string field, string parameterName)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"User not found in db by given {parameterName}: <{field}>", "UserNotFound");
	}
	
	public static ErtisAuthException UserInactive(string id)
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, $"User is inactive ({id})", "UserInactive");
	}
	
	public static ErtisAuthException InvalidCredentials()
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "Username or password is invalid", "InvalidCredentials");
	}
	
	public static ErtisAuthException PasswordRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Password is required", "PasswordRequired");
	}
	
	public static ErtisAuthException PasswordMinLengthRuleError(int minLength)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"The user password length must be a minimum of {minLength} characters.", "PasswordMinLengthRuleError");
	}
	
	public static ErtisAuthException EmailAddressRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Email address is required", "EmailAddressRequired");
	}
	
	#endregion
	
	#region User Type Exceptions
	
	public static ErtisAuthException UserTypeNameRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "User type name is required.", "UserTypeNameRequired");
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
	
	public static ErtisAuthException UserTypeInheritanceCycle(string baseUserTypeName)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"The base user type '{baseUserTypeName}' inherits from this user type (an inheritance cycle), or its inheritance chain is too deep.", "UserTypeInheritanceCycle");
	}
	
	public static ErtisAuthException InheritedTypeIsAbstract(string baseUserTypeName)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"The base user type '{baseUserTypeName}' has abstract modifier.", "InheritedTypeIsAbstract");
	}
	
	public static ErtisAuthException ReservedUserTypeName(string userTypeName)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"'{userTypeName}' is a reserved name. It's can not be used as user type name.", "ReservedUserTypeName");
	}
	
	public static ErtisAuthException ReservedUserTypeSlug(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"'{slug}' is a reserved slug. It's can not be used as user type slug.", "ReservedUserTypeSlug");
	}
	
	public static ErtisAuthException UniqueFieldHasDuplicates(string fieldPath, string duplicateKey)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The '{fieldPath}' field can not be unique, some users already share the same value. ({duplicateKey})", "UniqueFieldHasDuplicates");
	}
	
	public static ErtisAuthException UserTypeAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"'{slug}' is already exist.", "UserTypeAlreadyExists");
	}
	
	public static ErtisAuthException UserTypeNotFound(string field, string parameterName)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"User type not found in db by given {parameterName}: <{field}>", "UserTypeNotFound");
	}
	
	public static ErtisAuthException UserTypeRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "User type is required", "UserTypeRequired");
	}
	
	// ReSharper disable once UnusedParameter.Global
	public static ErtisAuthException UserTypeImmutable(IReadOnlyDictionary<string, object?>? extras = null)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "User type is an immutable field. It's cannot be updated.", "UserTypeImmutable");
	}
	
	public static ErtisAuthException UserTypeCanNotBeDelete()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "This user type currently using by another users, it's cannot be deleted.", "UserTypeCanNotBeDelete");
	}
	
	public static ErtisAuthException HostRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "The X-Host header required for this action", "HostRequired");
	}
	
	#endregion
	
	#region Application Exceptions
	
	public static ErtisAuthException ApplicationNotFound(string id)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"Application not found in db by given id: <{id}>", "ApplicationNotFound");
	}
	
	public static ErtisAuthException ApplicationAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The application is already exists with same slug ({slug})", "ApplicationAlreadyExists");
	}
	
	#endregion
	
	#region Role Exceptions
	
	public static ErtisAuthException RoleRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Role required", "RoleRequired");
	}
	
	public static ErtisAuthException RoleNotFound(string value, string? fieldName = null)
	{
		fieldName = string.IsNullOrEmpty(fieldName) ? "id" : fieldName;
		return new ErtisAuthException(HttpStatusCode.NotFound, $"Role not found in db by given {fieldName}: <{value}>", "RoleNotFound");
	}
	
	public static ErtisAuthException RoleAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The role is already exists with same slug ({slug})", "RoleAlreadyExists");
	}
	
	public static ErtisAuthException ReservedRole(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"This role is reserved by the system ({slug})", "ReservedRole");
	}
	
	public static ErtisAuthException SystemRolesCannotBeDeleted(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The reserved system roles cannot be deleted ({slug})", "SystemRolesCannotBeDeleted");
	}
	
	#endregion
	
	#region Provider Exceptions
	
	public static ErtisAuthException ProviderNotFound(string providerId)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"Provider not found in db by given _id: <{providerId}>", "ProviderNotFound");
	}
	
	public static ErtisAuthException ProviderAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The provider is already exists with same slug ({slug})", "ProviderAlreadyExists");
	}
	
	public static ErtisAuthException ProviderIsDisable()
	{
		return new ErtisAuthException(HttpStatusCode.Forbidden, "Provider is disable", "ProviderIsDisable");
	}
	
	public static ErtisAuthException UntrustedProvider()
	{
		return new ErtisAuthException(HttpStatusCode.Forbidden, "Untrusted provider", "UntrustedProvider");
	}
	
	public static ErtisAuthException ProviderEmailNotTrusted(string providerName)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"A user with the same email address already exists and the email address is not verified by {providerName}", "ProviderEmailNotTrusted");
	}
	
	public static ErtisAuthException ProviderProfileIncomplete(string providerName, string message)
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, $"The {providerName} profile is incomplete ({message})", "ProviderProfileIncomplete");
	}
	
	public static ErtisAuthException ProviderNotConfigured()
	{
		return new ErtisAuthException(HttpStatusCode.Forbidden, "Provider not configured", "ProviderNotConfigured");
	}
	
	public static ErtisAuthException ProviderNotConfiguredCorrectly(string message)
	{
		return new ErtisAuthException(HttpStatusCode.NotImplemented, $"Provider not configured correctly. ({message})", "ProviderNotConfiguredCorrectly");
	}
	
	public static ErtisAuthException ProviderUnavailable(string providerName)
	{
		return new ErtisAuthException(HttpStatusCode.ServiceUnavailable, $"{providerName} could not be reached, please try again later", "ProviderUnavailable");
	}
	
	public static ErtisAuthException AuthenticationServiceUnavailable()
	{
		return new ErtisAuthException(HttpStatusCode.ServiceUnavailable, "The authentication service could not be reached, please try again later", "AuthenticationServiceUnavailable");
	}
	
	public static ErtisAuthException UnsupportedProvider()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Provider is not supported", "UnsupportedProvider");
	}
	
	public static ErtisAuthException UnknownProvider(string name)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"Unknown provider: {name}", "UnknownProvider");
	}
	
	public static ErtisAuthException ProviderTypeRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "Provider type is required", "ProviderTypeRequired");
	}
	
	public static ErtisAuthException ProviderSlugCannotBeChanged(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"The slug of a provider can not be changed ({slug}); it's used in the login url and in the users' connected accounts", "ProviderSlugCannotBeChanged");
	}
	
	public static ErtisAuthException InvalidProviderLoginRequest(string providerType, string? message = null)
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, $"The request body is not a valid {providerType} login request" + (string.IsNullOrEmpty(message) ? string.Empty : $" ({message})"), "InvalidProviderLoginRequest");
	}
	
	#endregion
	
	#region Webhook Exceptions
	
	public static ErtisAuthException WebhookNotFound(string webhookId)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"Webhook not found in db by given _id: <{webhookId}>", "WebhookNotFound");
	}
	
	public static ErtisAuthException WebhookAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The webhook is already exists with same slug ({slug})", "WebhookAlreadyExists");
	}
	
	#endregion
	
	#region MailHook Exceptions
	
	public static ErtisAuthException MailHookNotFound(string id)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"Mail hook not found in db by given _id: <{id}>", "MailHookNotFound");
	}
	
	public static ErtisAuthException MailHookAlreadyExists(string slug)
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, $"The mailhook is already exists with same slug ({slug})", "MailHookAlreadyExists");
	}
	
	public static ErtisAuthException ActivationMailHookWasNotDefined()
	{
		return new ErtisAuthException(HttpStatusCode.NotImplemented, "The activation mail hook was not defined or not configured correctly or passive", "ActivationMailHookWasNotDefined");
	}
	
	public static ErtisAuthException ResetPasswordMailHookWasNotDefined()
	{
		return new ErtisAuthException(HttpStatusCode.NotImplemented, "The reset password mail hook was not defined or not configured correctly or passive", "ResetPasswordMailHookWasNotDefined");
	}
	
	public static ErtisAuthException NotDefinedAnyMailProvider()
	{
		return new ErtisAuthException(HttpStatusCode.NotImplemented, "No mail provider has been defined yet", "NotDefinedAnyMailProvider");
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
	
	public static ErtisAuthException BulkDeleteFailed(IEnumerable<string>? ids)
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
		return new ErtisAuthException(HttpStatusCode.OK, "Bulk delete operation was partial completed", "BulkDeletePartial");
	}
	
	#endregion
	
	#region Setup Exceptions
	
	public static ErtisAuthException SetupRejected(string message)
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, message, "SetupRejected");
	}
	
	public static ErtisAuthException AlreadySetUp()
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, "ErtisAuth has already been set up", "AlreadySetUp");
	}
	
	public static ErtisAuthException SetupInProgress()
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, "Another setup request is in progress", "SetupInProgress");
	}
	
	#endregion
	
	#region OTP Exceptions
	
	public static ErtisAuthException OneTimePasswordNotFound(string id)
	{
		return new ErtisAuthException(HttpStatusCode.NotFound, $"OTP not found in db by given _id: <{id}>", "OneTimePasswordNotFound");
	}
	
	public static ErtisAuthException OtpHostRequired()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "The host is required in one time password configuration", "OtpHostRequired");
	}
	
	public static ErtisAuthException OtpHostMismatch()
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "The provided host info and the host info in the membership configuration do not match", "OtpHostMismatch");
	}
	
	public static ErtisAuthException OtpNotConfiguredYet()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "The one time password has not configured yet", "OtpNotConfiguredYet");
	}
	
	public static ErtisAuthException OtpHostNotConfiguredYet()
	{
		return new ErtisAuthException(HttpStatusCode.BadRequest, "The one time password host info has not configured yet", "OtpHostNotConfiguredYet");
	}
	
	public static ErtisAuthException OtpExpired()
	{
		return new ErtisAuthException(HttpStatusCode.Unauthorized, "One time password was expired", "OtpExpired");
	}
	
	public static ErtisAuthException OneTimePasswordAlreadyExists()
	{
		return new ErtisAuthException(HttpStatusCode.Conflict, "The otp is already exists", "OneTimePasswordAlreadyExists");
	}
	
	#endregion
}