using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.WebAPI.Extensions;

public class RequestLoggingMiddleware
{
	#region Fields
	
	private readonly RequestDelegate _next;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="next"></param>
	public RequestLoggingMiddleware(RequestDelegate next)
	{
		this._next = next;
	}
	
	#endregion
	
	#region Methods
	
	public async Task Invoke(HttpContext context)
	{
		var request = context.Request;
		Console.WriteLine($"[{DateTime.Now}] {request.Method} {request.Path}");
		await this._next(context);
	}
	
	#endregion
}

public static class RequestLoggingMiddlewareExtensions
{
	public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<RequestLoggingMiddleware>();
	}
}