using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Applications;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IApplicationRepository : IMongoRepository<ApplicationDto>
	{
		
	}
}