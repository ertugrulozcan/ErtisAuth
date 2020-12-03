using System;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}")]
	public class HealthCheckController : ControllerBase
	{
		#region Methods

		[HttpGet("healthcheck")]
		public IActionResult HealthCheck()
		{
			try
			{
				return this.Ok();
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