using System;
using System.Linq;
using System.Threading.Tasks;
using Ertis.MongoDB.Database;
using ErtisAuth.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}")]
	public class HealthCheckController : ControllerBase
	{
		#region Services

		private readonly IMongoDatabase database;
		private readonly IMembershipService membershipService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="database"></param>
		/// <param name="membershipService"></param>
		public HealthCheckController(IMongoDatabase database, IMembershipService membershipService)
		{
			this.database = database;
			this.membershipService = membershipService;
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
					dbStatisticsTask.AsTask(),
					listCollectionsTask.AsTask()
				};

				Task.WaitAll(tasks);

				var dbStatistics = await dbStatisticsTask;
				if (dbStatistics == null)
				{
					return this.Ok(new
					{
						Status = "Unhealthy",
						Message = "Database statistics could not fetched"
					});
				}
			
				var memberships = await this.membershipService.GetAsync();
				
				var collectionList = (await listCollectionsTask).ToList();
				if (!collectionList.Contains("memberships") ||
					!collectionList.Contains("roles") ||
					!collectionList.Contains("users") ||
					!memberships.Items.Any())
				{
					return this.Ok(new
					{
						Status = "Unhealthy",
						Message = "Database have not migrated yet"
					});
				}
				
				return this.Ok(new
				{
					Status = "Healthy"
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
		
		[HttpGet("build-id")]
		public IActionResult BuildId()
		{
			return this.Ok("9.0.5.1");
		}

		#endregion
	}
}