using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IUserService : IMembershipBoundedCrudService<User>
	{
		UserWithPassword GetUserWithPassword(string id, string membershipId);
		
		Task<UserWithPassword> GetUserWithPasswordAsync(string id, string membershipId);
		
		UserWithPassword GetUserWithPassword(string username, string email, string membershipId);
		
		Task<UserWithPassword> GetUserWithPasswordAsync(string username, string email, string membershipId);
		
		Task<User> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword);
	}
}