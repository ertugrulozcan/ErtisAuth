using Ertis.Data.Repository;
using ErtisAuth.Dto.Models;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface ITestRepository : IRepository<TestModelDto, string>
	{
		
	}
}