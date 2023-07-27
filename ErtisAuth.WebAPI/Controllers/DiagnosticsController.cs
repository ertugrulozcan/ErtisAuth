using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Route("api/v{v:apiVersion}/diagnostics")]
public class DiagnosticsController : ControllerBase
{
	#region Methods

	[HttpGet("memory")]
	public IActionResult GetMemory()
	{
		using (var process = Process.GetCurrentProcess())
		{
			var paged = process.PagedMemorySize64;
			var @private = process.PrivateMemorySize64;
			var @virtual = process.VirtualMemorySize64;
			var system = process.NonpagedSystemMemorySize64;
			var total = GC.GetTotalMemory(false);
			var gc = GC.GetGCMemoryInfo();
			
			return this.Ok(new
			{
				paged, 
				@private, 
				@virtual, 
				system, 
				total, 
				gc
			});
		}
	}

	#endregion
}