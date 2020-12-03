using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models;

namespace ErtisAuth.Dao.Repositories
{
	public class TestRepository : MongoRepositoryBase<TestModelDto>, ITestRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		public TestRepository(IDatabaseSettings settings) : base(settings, "test")
		{
			
		}

		#endregion
	}
}