using System.Threading.Tasks;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IUserService : IMembershipBoundedCrudService<User>
	{
		User GetByUsernameOrEmail(string username, string email, string membershipId);
		
		Task<User> GetByUsernameOrEmailAsync(string username, string email, string membershipId);
	}
}