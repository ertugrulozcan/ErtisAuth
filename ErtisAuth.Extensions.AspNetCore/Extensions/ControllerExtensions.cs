using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Constants;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Extensions.Authorization.Extensions;
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
			return TokenBase.ExtractToken(authorizationHeader, out tokenType);
		}
		
		public static string GetTokenFromHeader(this ControllerBase controller, out string tokenType)
		{
			var authorizationHeader = controller.GetAuthorizationHeader();
			return TokenBase.ExtractToken(authorizationHeader, out tokenType);
		}

		public static Utilizer GetUtilizer(this ControllerBase controller, bool fallbackWithToken = true)
		{
			var claimUser = controller.User;
			var utilizerIdentity = claimUser.Identities.FirstOrDefault(x => x.NameClaimType == "Utilizer");
			if (utilizerIdentity != null)
			{
				return utilizerIdentity.ConvertToUtilizer();
			}
			else if (fallbackWithToken)
			{
				var token = controller.GetTokenFromHeader(out var tokenTypeString);
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
							Token = token,
							TokenType = SupportedTokenTypes.Basic
						};
					}
					case SupportedTokenTypes.Bearer:
					{
						var handler = new JwtSecurityTokenHandler();
						var jwt = handler.ReadToken(token) as JwtSecurityToken;
						return jwt.ConvertToUtilizer();
					}
				}
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