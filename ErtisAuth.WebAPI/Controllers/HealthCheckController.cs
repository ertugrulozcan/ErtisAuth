using System;
using System.Linq;
using System.Threading.Tasks;
using Ertis.MongoDB.Database;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}")]
	public class HealthCheckController : ControllerBase
	{
		#region Services

		private readonly IMongoDatabase database;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="database"></param>
		public HealthCheckController(IMongoDatabase database)
		{
			this.database = database;
		}

		#endregion
		
		#region Methods

		[HttpGet("healthcheck")]
		public async Task<IActionResult> HealthCheck()
		{
			try
			{
				var dbStatisticsTask = this.database.GetDatabaseStatisticsAsync();
				var listCollectionsTask = this.database.ListCollectionsAsync();
				
				var tasks = new Task[]
				{
					dbStatisticsTask,
					listCollectionsTask
				};

				Task.WaitAll(tasks);

				var dbStatistics = await dbStatisticsTask;
				if (dbStatistics == null)
				{
					return this.Problem("Database statistics could not fetched!");
				}
			
				var collectionList = (await listCollectionsTask).ToList();
				if (!collectionList.Contains("memberships") ||
					!collectionList.Contains("roles") ||
					!collectionList.Contains("users"))
				{
					return this.Problem("Database have not migrated yet!");
				}
				
				return this.Ok(new
				{
					collectionList
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return this.StatusCode(500, ex);
			}
		}

		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return this.Ok("Pong");
		}

		#endregion
	}
}