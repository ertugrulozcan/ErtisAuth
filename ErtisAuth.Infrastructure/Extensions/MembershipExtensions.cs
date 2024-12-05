using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ertis.Security.Cryptography;
using Ertis.Security.Helpers;
using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Infrastructure.Extensions
{
	public static class MembershipExtensions
	{
		#region Method

		public static HashAlgorithms GetHashAlgorithm(this Membership membership)
		{
			if (string.IsNullOrEmpty(membership.HashAlgorithm))
			{
				return Core.Constants.Defaults.DEFAULT_HASH_ALGORITHM;
			}
			
			if (!HashParser.TryParseHashAlgorithm(membership.HashAlgorithm, out var algorithm, out _, out _))
			{
				algorithm = Core.Constants.Defaults.DEFAULT_HASH_ALGORITHM;
				Console.WriteLine($"Membership hash algorithm could not parsed! ('{membership.HashAlgorithm}')");
			}

			return algorithm;
		}
		
		public static Encoding GetEncoding(this Membership membership)
		{
			if (string.IsNullOrEmpty(membership.DefaultEncoding))
			{
				return Core.Constants.Defaults.DEFAULT_ENCODING;
			}
			
			var encoding = Core.Constants.Defaults.DEFAULT_ENCODING;
			var encodings = Encoding.GetEncodings();
			var encodingInfo = encodings.FirstOrDefault(x => x.Name == membership.DefaultEncoding.ToLower());
			if (encodingInfo != null)
			{
				encoding = encodingInfo.GetEncoding();
			}
			else
			{
				Console.WriteLine($"Membership encoding could not parsed! ('{membership.DefaultEncoding}')");
			}

			return encoding;
		}

		public static bool IsValid(this Membership membership, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (membership.ExpiresIn <= 0)
			{
				errorList.Add("expires_in is not set");
			}
			
			if (membership.RefreshTokenExpiresIn <= 0)
			{
				errorList.Add("refresh_token_expires_in is not set");
			}
			
			if (membership.ResetPasswordTokenExpiresIn < 0)
			{
				errorList.Add("reset_password_token_expires_in is not valid");
			}
			
			if (string.IsNullOrEmpty(membership.SecretKey))
			{
				errorList.Add("secret_key is not set");
			}
			
			if (string.IsNullOrEmpty(membership.HashAlgorithm))
			{
				errorList.Add("hash_algorithm is not set");
			}
			
			if (string.IsNullOrEmpty(membership.DefaultEncoding))
			{
				errorList.Add("encoding is not set");
			}

			errors = errorList;
			return !errorList.Any();
		}

		#endregion
	}
}