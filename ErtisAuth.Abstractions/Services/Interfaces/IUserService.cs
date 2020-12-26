using System.Threading.Tasks;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IUserService : IMembershipBoundedCrudService<User>
	{
		UserWithPassword GetUserWithPassword(string username, string email, string membershipId);
		
		Task<UserWithPassword> GetUserWithPasswordAsync(string username, string email, string membershipId);
	}
}