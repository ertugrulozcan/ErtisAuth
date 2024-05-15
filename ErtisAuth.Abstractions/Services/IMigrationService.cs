using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services
{
	public interface IMigrationService
	{
		ValueTask<dynamic> MigrateAsync(string connectionString, Membership _membership, UserWithPassword _user, Application _application);
	}
}