using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ErtisAuth.WebAPI.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Route("api/v{v:apiVersion}/diagnostics")]
public class DiagnosticsController : ControllerBase
{
	#region Methods

	[HttpGet("memory")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	public IActionResult GetMemoryUsage()
	{
		using (var process = Process.GetCurrentProcess())
		{
			var paged = process.PagedMemorySize64;
			var @private = process.PrivateMemorySize64;
			var @virtual = process.VirtualMemorySize64;
			var system = process.NonpagedSystemMemorySize64;
			var total = GC.GetTotalMemory(false);
			var used = SystemDiagnostics.GetUsedMemoryForAllProcesses();
			var size = SystemDiagnostics.GetTotalMemoryInKb() * 1024;
			var gc = GC.GetGCMemoryInfo();

			return this.Ok(new
			{
				paged, 
				@private, 
				@virtual, 
				system, 
				total, 
				used,
				size, 
				gc
			});
		}
	}
	
	[HttpGet("cpu")]
	public IActionResult GetCpuUsage()
	{
		return this.Ok(new
		{
			overall = SystemDiagnostics.GetOverallCpuUsage()
		});
	}

	#endregion
}