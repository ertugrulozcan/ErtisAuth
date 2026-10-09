using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Extensions.Authorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.Sdk.AspNetCore.Extensions;

public static class ControllerExtensions
{
	#region Methods
	
	public static string? GetAuthorizationHeader(this HttpRequest request)
	{
		if (request.Headers.ContainsKey("Authorization"))
		{
			return request.Headers["Authorization"];
		}
		
		return null;
	}
	
	public static string? GetAuthorizationHeader(this ControllerBase controller)
	{
		if (controller.Request.Headers.ContainsKey("Authorization"))
		{
			return controller.Request.Headers["Authorization"];
		}
		
		return null;
	}
	
	public static string? GetTokenFromHeader(this HttpRequest request, out string? tokenType)
	{
		var authorizationHeader = request.GetAuthorizationHeader();
		return TokenBase.ExtractToken(authorizationHeader, out tokenType);
	}
	
	public static string? GetTokenFromHeader(this ControllerBase controller, out string? tokenType)
	{
		var authorizationHeader = controller.GetAuthorizationHeader();
		return TokenBase.ExtractToken(authorizationHeader, out tokenType);
	}
	
	/// <summary>
	/// The caller authenticated by ErtisAuth: set on the endpoints of an [Authorized] controller and on [SelfAuthorized] endpoints.
	/// Null on the endpoints which are not authenticated ([Unauthorized], or without any ErtisAuth attribute).
	/// </summary>
	public static Utilizer? GetUtilizer(this ControllerBase controller)
	{
		var utilizerIdentity = controller.User.Identities.FirstOrDefault(x => x.NameClaimType == ClaimExtensions.UtilizerClaimName);
		return utilizerIdentity?.ConvertToUtilizer();
	}
	
	/// <summary>
	/// The caller authenticated by ErtisAuth, or, on the endpoints which are not authenticated, the caller the token of the
	/// request claims to be: a Bearer token is read without checking its signature, expiry or revocation, a Basic token
	/// without checking its secret. **Note:** anyone can send a token claiming any identity; never use the result of this
	/// method for authorization decisions, use GetUtilizer on an [Authorized] or [SelfAuthorized] endpoint instead.
	/// Throws when the request has no token or an unsupported token type.
	/// </summary>
	public static Utilizer? GetUnverifiedUtilizer(this ControllerBase controller)
	{
		var utilizer = controller.GetUtilizer();
		if (utilizer != null)
		{
			return utilizer;
		}
		else
		{
			var token = controller.GetTokenFromHeader(out var tokenTypeString);
			if (string.IsNullOrEmpty(token))
			{
				throw ErtisAuthException.InvalidToken("Authorization token not found");
			}
			
			if (string.IsNullOrEmpty(tokenTypeString) || !TokenTypeExtensions.TryParseTokenType(tokenTypeString, out var tokenType) || tokenType == SupportedTokenTypes.None)
			{
				throw ErtisAuthException.UnsupportedTokenType();
			}
			
			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
			switch (tokenType)
			{
				case SupportedTokenTypes.Basic:
				{
					var applicationId = token.Split(':')[0];
					return new Utilizer
					{
						Id = applicationId,
						Type = Utilizer.UtilizerType.Application,
						Username = applicationId,
						Token = token,
						TokenType = SupportedTokenTypes.Basic,
						MembershipId = string.Empty
					};
				}
				case SupportedTokenTypes.Bearer:
				{
					var tokenHandler = new JsonWebTokenHandler();
					var jwt = tokenHandler.ReadJsonWebToken(token);
					return jwt.ConvertToUtilizer();
				}
			}
		}
		
		return null;
	}
	
	public static UnauthorizedObjectResult AuthorizationHeaderMissing(this ControllerBase controller)
	{
		return controller.Unauthorized(ErtisAuthException.AuthorizationHeaderMissing().Error);
	}
	
	public static UnauthorizedObjectResult InvalidToken(this ControllerBase controller)
	{
		return controller.Unauthorized(ErtisAuthException.InvalidToken().Error);
	}
	
	#endregion
}