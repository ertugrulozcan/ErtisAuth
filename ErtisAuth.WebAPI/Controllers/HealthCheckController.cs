using Ertis.MongoDB.Database;
using ErtisAuth.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Health Check")]
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
	
	/// <summary>Health check</summary>
	/// <remarks>Checks the database connection and whether the installation is set up. Anonymous. **Note:** an installation which is not set up yet answers 200 with the status <c>Unhealthy</c>; only a failing database answers 500.</remarks>
	[HttpGet("healthcheck")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken = default)
	{
		try
		{
			var collectionList = (await this._database.ListCollectionsAsync(cancellationToken: cancellationToken)).ToList();
			if (!collectionList.Contains("memberships") ||
				!collectionList.Contains("roles") ||
				!collectionList.Contains("users") ||
				!await this._setupService.IsSetUpAsync(cancellationToken: cancellationToken))
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
	
	/// <summary>Ping</summary>
	/// <remarks>Answers <c>Pong</c>. Anonymous; does not touch the database.</remarks>
	[HttpGet("ping")]
	[ProducesResponseType<string>(StatusCodes.Status200OK)]
	public IActionResult Ping()
	{
		return this.Ok("Pong");
	}
	
	#endregion
}