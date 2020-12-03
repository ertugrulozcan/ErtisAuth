using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface ITestRepository : IMongoRepository<TestModelDto>
	{
		
	}
}