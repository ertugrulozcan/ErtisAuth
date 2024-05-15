using Ertis.Security.Cryptography;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Infrastructure.Extensions;

namespace ErtisAuth.Infrastructure.Services
{
	public class CryptographyService : ICryptographyService
	{
		#region Methods

		public string CalculatePasswordHash(Membership membership, string password)
		{
			if (string.IsNullOrEmpty(password))
			{
				return password;
			}
			
			var hashProvider = new HashProvider();
			var algorithm = membership.GetHashAlgorithm();
			var encoding = membership.GetEncoding();
			var passwordHash = hashProvider.Hash(password, algorithm, encoding);
			return passwordHash;
		}

		#endregion
	}
}