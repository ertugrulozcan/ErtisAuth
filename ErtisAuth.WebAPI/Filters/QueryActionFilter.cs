using System.Text.Json;
using Ertis.Core.Models;
using Ertis.Extensions.AspNetCore.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErtisAuth.WebAPI.Filters;

/// <summary>
/// Error responses of the _query endpoints (Ertis QueryControllerBase.Query), in line with the global exception handler:
/// a body that is not valid JSON is rejected with 400 InvalidQuery, and an unexpected error, which Query answers
/// itself with 500 and the exception message, gets the generic 500 body (the message is logged).
/// </summary>
public class QueryActionFilter : IAsyncActionFilter
{
	#region Services
	
	private readonly ILogger<QueryActionFilter> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="logger"></param>
	public QueryActionFilter(ILogger<QueryActionFilter> logger)
	{
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		if (context.Controller is not QueryControllerBase || !context.ActionDescriptor.RouteValues.TryGetValue("action", out var action) || action != nameof(QueryControllerBase.Query))
		{
			await next();
			return;
		}
		
		var request = context.HttpContext.Request;
		request.EnableBuffering();
		
		using (var reader = new StreamReader(request.Body, leaveOpen: true))
		{
			var body = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
			request.Body.Position = 0;
			
			if (!string.IsNullOrWhiteSpace(body) && !IsValidJson(body))
			{
				context.Result = new BadRequestObjectResult(new ErrorModel
				{
					Message = "The request body is not a valid JSON document",
					ErrorCode = "InvalidQuery",
					StatusCode = 400
				});
				
				return;
			}
		}
		
		var executed = await next();
		if (executed.Result is ObjectResult { StatusCode: StatusCodes.Status500InternalServerError, Value: string message })
		{
			this._logger.LogError("Query failed on {Path}: {Message}", request.Path, message);
			executed.Result = new ObjectResult(new ErrorModel
			{
				Message = "An unexpected error occurred",
				ErrorCode = "UnhandledExceptionError",
				StatusCode = 500
			})
			{
				StatusCode = StatusCodes.Status500InternalServerError
			};
		}
	}
	
	private static bool IsValidJson(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return false;
		}
		
		try
		{
			using var document = JsonDocument.Parse(body);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}
	
	#endregion
}