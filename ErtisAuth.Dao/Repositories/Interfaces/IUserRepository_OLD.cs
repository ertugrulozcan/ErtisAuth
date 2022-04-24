using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IUserRepository_OLD : IMongoRepository<UserDto>
	{
		
	}
}