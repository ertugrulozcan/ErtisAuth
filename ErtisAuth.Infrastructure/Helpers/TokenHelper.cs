using System;
using System.Linq;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Infrastructure.Exceptions;

namespace ErtisAuth.Infrastructure.Helpers
{
	public static class TokenHelper
	{
		public static string ExtractToken(string authorizationHeader, out string tokenType)
		{
			if (string.IsNullOrEmpty(authorizationHeader))
			{
				tokenType = null;
				return null;
			}

			var parts = authorizationHeader.Split(' ');
			if (parts.Length > 2)
			{
				throw ErtisAuthException.InvalidToken();
			}

			if (parts.Length == 2)
			{
				var supportedTokenTypes = Enum.GetValues(typeof(SupportedTokenTypes)).Cast<SupportedTokenTypes>().Select(x => x.ToString());
				if (!supportedTokenTypes.Contains(parts[0]))
				{
					throw ErtisAuthException.UnsupportedTokenType();
				}

				tokenType = parts[0];
				return parts[1];
			}
			
			tokenType = null;
			return parts[0];
		}
	}
}