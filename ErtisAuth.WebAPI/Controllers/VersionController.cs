using ErtisAuth.WebAPI.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}")]
	public class VersionController : ControllerBase
	{
		#region Methods

		[HttpGet("version")]
		public IActionResult GetVersion()
		{
			return this.Ok(EnvironmentParams.GetEnvironmentParameter("Version"));
		}

		#endregion
	}
}