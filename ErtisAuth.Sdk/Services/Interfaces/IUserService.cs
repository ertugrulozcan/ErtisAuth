using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IUserService
	{
		IResponseResult<User> CreateUser(UserWithPassword user, TokenBase token);
		
		Task<IResponseResult<User>> CreateUserAsync(UserWithPassword user, TokenBase token);
		
		IResponseResult<User> GetUser(string userId, TokenBase token);
		
		Task<IResponseResult<User>> GetUserAsync(string userId, TokenBase token);
		
		IResponseResult<IPaginationCollection<User>> GetUsers(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<User>>> GetUsersAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		IResponseResult<IPaginationCollection<User>> QueryUsers(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<User>>> QueryUsersAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		IResponseResult<User> UpdateUser(User user, TokenBase token);
		
		Task<IResponseResult<User>> UpdateUserAsync(User user, TokenBase token);
		
		IResponseResult DeleteUser(string userId, TokenBase token);
		
		Task<IResponseResult> DeleteUserAsync(string userId, TokenBase token);
	}
}