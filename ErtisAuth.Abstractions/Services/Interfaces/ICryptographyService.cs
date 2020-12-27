using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface ICryptographyService
	{
		string CalculatePasswordHash(Membership membership, string password);
	}
}