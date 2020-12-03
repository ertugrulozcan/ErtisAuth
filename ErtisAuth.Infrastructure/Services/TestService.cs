using System.Linq;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models;

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

		public async Task<IPaginationCollection<TestModel>> GetAsync(string query, int? skip = null, int? limit = null, bool? withCount = null, string sortField = null, SortDirection? sortDirection = null)
		{
			var dtos = await this.testRepository.QueryAsync(query, skip, limit, withCount, sortField, sortDirection);
			if (dtos?.Items == null)
			{
				return null;
			}

			return new PaginationCollection<TestModel>
			{
				Count = dtos.Count,
				Items = dtos.Items.Select(ConvertToModel)
			};
		}

		public static TestModel ConvertToModel(TestModelDto dto)
		{
			return new TestModel
			{
				Id = dto.Id,
				Text = dto.Text,
				Integer = dto.Integer,
				Double = dto.Double,
				Array = dto.Array?.Select(ConvertToModel).ToArray(),
				Enum = dto.Enum,
				NullableDate = dto.NullableDate
			};
		}

		#endregion
	}
}