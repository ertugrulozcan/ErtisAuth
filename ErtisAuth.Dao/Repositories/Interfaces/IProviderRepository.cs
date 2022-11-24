using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Providers;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IProviderRepository : IMongoRepository<ProviderDto>
	{
		
	}
}