using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/[controller]")]
	public class TestController : QueryControllerBase<TestModel>
	{
		#region Services

		private readonly ITestService testService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="testService"></param>
		public TestController(ITestService testService)
		{
			this.testService = testService;
		}

		#endregion
		
		#region Methods

		protected override async Task<IPaginationCollection<TestModel>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection)
		{
			return await this.testService.GetAsync(query, skip, limit, withCount, sortField, sortDirection);
		}

		#endregion
	}
}