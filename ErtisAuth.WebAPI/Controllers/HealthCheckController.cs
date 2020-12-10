using System;
using ErtisAuth.WebAPI.Extensions;
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
			return this.Ok();
		}

		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return this.Ok("Pong");
		}

		#endregion
	}
}