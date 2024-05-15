using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Abstractions.Services
{
	public interface ICryptographyService
	{
		string CalculatePasswordHash(Membership membership, string password);
	}
}