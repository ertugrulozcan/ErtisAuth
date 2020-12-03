using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Dao.Repositories.Interfaces;

namespace ErtisAuth.Infrastructure.Services
{
	public class TestService : ITestService
	{
		#region Services

		private readonly ITestRepository testRepository;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="testRepository"></param>
		public TestService(ITestRepository testRepository)
		{
			this.testRepository = testRepository;
		}

		#endregion
		
		#region Methods

		public async Task<IPaginationCollection<dynamic>> GetAsync(
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string sortField = null, 
			SortDirection? sortDirection = null,
			IDictionary<string, bool> selectFields = null)
		{
			return await this.testRepository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
	}
}