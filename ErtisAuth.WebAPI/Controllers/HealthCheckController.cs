using Ertis.MongoDB.Database;
using ErtisAuth.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
public class HealthCheckController : ControllerBase
{
	#region Services
	
	private readonly IMongoDatabase _database;
	private readonly ISetupService _setupService;
	private readonly ILogger<HealthCheckController> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="database"></param>
	/// <param name="setupService"></param>
	/// <param name="logger"></param>
	public HealthCheckController(IMongoDatabase database, ISetupService setupService, ILogger<HealthCheckController> logger)
	{
		this._database = database;
		this._setupService = setupService;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	[HttpGet("healthcheck")]
	public async Task<IActionResult> HealthCheck()
	{
		try
		{
			var dbStatisticsTask = this._database.GetDatabaseStatisticsAsync();
			var listCollectionsTask = this._database.ListCollectionsAsync();
			
			await Task.WhenAll(dbStatisticsTask, listCollectionsTask);
			
			var dbStatistics = await dbStatisticsTask;
			if (dbStatistics == null)
			{
				return this.Ok(new
				{
					Status = "Unhealthy",
					Message = "Database statistics could not fetched"
				});
			}
			
			var collectionList = (await listCollectionsTask).ToList();
			if (!collectionList.Contains("memberships") ||
				!collectionList.Contains("roles") ||
				!collectionList.Contains("users") ||
				!await this._setupService.IsSetUpAsync())
			{
				return this.Ok(new
				{
					Status = "Unhealthy",
					Message = "ErtisAuth has not been set up yet"
				});
			}
			
			return this.Ok(new
			{
				Status = "Healthy"
			});
		}
		catch (Exception ex)
		{
			// The endpoint is anonymous: the exception (stack trace, connection details) stays in the log
			this._logger.LogError(ex, "Health check failed");
			return this.StatusCode(500, new
			{
				Status = "Unhealthy",
				Message = "Health check failed"
			});
		}
	}
	
	[HttpGet("ping")]
	public IActionResult Ping()
	{
		return this.Ok("Pong");
	}
	
	#endregion
}