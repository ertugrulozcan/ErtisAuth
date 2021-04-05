using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IUserService : IMembershipBoundedCrudService<User>
	{
		UserWithPasswordHash GetUserWithPassword(string id, string membershipId);
		
		Task<UserWithPasswordHash> GetUserWithPasswordAsync(string id, string membershipId);
		
		UserWithPasswordHash GetUserWithPassword(string username, string email, string membershipId);
		
		Task<UserWithPasswordHash> GetUserWithPasswordAsync(string username, string email, string membershipId);

		User ChangePassword(Utilizer utilizer, string membershipId, string userId, string newPassword);
		
		Task<User> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword);

		ResetPasswordToken ResetPassword(Utilizer utilizer, string membershipId, string usernameOrEmailAddress);
		
		Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string usernameOrEmailAddress);
		
		void SetPassword(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password);
		
		Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password);
	}
}