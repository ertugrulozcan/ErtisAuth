using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IUserService : IMembershipBoundedCrudService<User>
	{
		UserWithPasswordHash GetUserWithPassword(string id, string membershipId);
		
		ValueTask<UserWithPasswordHash> GetUserWithPasswordAsync(string id, string membershipId);
		
		UserWithPasswordHash GetUserWithPassword(string username, string email, string membershipId);
		
		ValueTask<UserWithPasswordHash> GetUserWithPasswordAsync(string username, string email, string membershipId);

		User ChangePassword(Utilizer utilizer, string membershipId, string userId, string newPassword);
		
		ValueTask<User> ChangePasswordAsync(Utilizer utilizer, string membershipId, string userId, string newPassword);

		ResetPasswordToken ResetPassword(Utilizer utilizer, string membershipId, string usernameOrEmailAddress);
		
		ValueTask<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string usernameOrEmailAddress);
		
		void SetPassword(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password);
		
		ValueTask SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password);
	}
}