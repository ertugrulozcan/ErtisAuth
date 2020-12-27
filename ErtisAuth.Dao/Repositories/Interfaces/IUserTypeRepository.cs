using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.UserTypes;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IUserTypeRepository : IMongoRepository<UserTypeDto>
	{
		
	}
}