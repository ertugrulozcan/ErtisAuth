using System.Collections.Generic;
using System.Linq;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Infrastructure.Extensions
{
	public static class ValidationExtensions
	{
		#region User Validation

		public static bool ValidateForPost(this User user, out IEnumerable<string> errors)
		{
			var errorList = new List<string>();
			if (string.IsNullOrEmpty(user.Username))
			{
				errorList.Add("'username' is a required field");
			}
			
			if (string.IsNullOrEmpty(user.EmailAddress))
			{
				errorList.Add("'email_address' is a required field");
			}

			errors = errorList;
			return !errors.Any();
		}

		#endregion
	}
}