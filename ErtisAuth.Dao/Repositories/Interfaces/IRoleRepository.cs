using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Roles;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IRoleRepository : IMongoRepository<RoleDto>
	{
		
	}
}