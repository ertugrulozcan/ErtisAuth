using System.Net;
using System.Text.Json;
using Ertis.Core.Exceptions;
using Ertis.Core.Models;
using Ertis.Schema.Exceptions;
using ErtisAuth.Core.Extensions;
using Microsoft.AspNetCore.Diagnostics;

namespace ErtisAuth.WebAPI.Extensions;

public static class ErrorHandlingExtensions
{
	public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
	{
		app.UseExceptionHandler(appError =>
		{
			appError.Run(async context =>
			{
				var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
				if (contextFeature?.Error != null)
				{
					context.Response.ContentType = "application/json";
					
					object errorModel;
					switch (contextFeature.Error)
					{
						case ValidationException validationException:
							context.Response.StatusCode = (int) validationException.StatusCode;
							errorModel = new ErrorModel<IEnumerable<string>>
							{
								Message = contextFeature.Error.Message,
								ErrorCode = validationException.ErrorCode,
								StatusCode = (int) validationException.StatusCode,
								Data = validationException.Errors
							};
							break;
						case CumulativeValidationException cumulativeValidationException:
							context.Response.StatusCode = 400;
							errorModel = new 
							{
								contextFeature.Error.Message,
								ErrorCode = "ValidationException",
								StatusCode = 400,
								Errors = cumulativeValidationException.Errors.Select(x => new
								{
									message = x.Message,
									fieldName = x.FieldName,
									fieldPath = x.FieldPath
								})
							};
							break;
						case ErtisException ertisException:
							context.Response.StatusCode = (int) ertisException.StatusCode;
							errorModel = new ErrorModel
							{
								Message = contextFeature.Error.Message,
								ErrorCode = ertisException.ErrorCode,
								StatusCode = (int) ertisException.StatusCode
							};
							break;
						case HttpStatusCodeException httpStatusCodeException:
							context.Response.StatusCode = (int) httpStatusCodeException.StatusCode;
							errorModel = new ErrorModel
							{
								Message = contextFeature.Error.Message,
								ErrorCode = httpStatusCodeException.HelpLink ?? "HttpStatusCodeError",
								StatusCode = (int) httpStatusCodeException.StatusCode
							};
							break;
						case ErtisSchemaValidationException ertisSchemaValidationException:
							context.Response.StatusCode = 400;
							errorModel = contextFeature.Error switch
							{
								FieldValidationException fieldValidationException => new
								{
									message = fieldValidationException.Message,
									fieldName = fieldValidationException.FieldName,
									fieldPath = fieldValidationException.FieldPath,
									errorCode = "FieldValidationException",
									statusCode = 400
								},
								SchemaValidationException schemaValidationException => new
								{
									message = schemaValidationException.Message,
									errorCode = "SchemaValidationException",
									statusCode = 400
								},
								_ => new ErrorModel
								{
									Message = ertisSchemaValidationException.Message,
									ErrorCode = "SchemaValidationException",
									StatusCode = 400
								}
							};
							break;
						default:
						{
							// Invalid ObjectId
							if (contextFeature.Error.IsObjectIdParseException(out var actualValue))
							{
								context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
								errorModel = new ErrorModel
								{
									Message = string.IsNullOrEmpty(actualValue) ? "Invalid id parameter" : $"'{actualValue}' is not a valid id parameter",
									ErrorCode = "ParameterFormatError",
									StatusCode = 400
								};
								
								break;
							}
							
							// Internal Server Error (500)
							var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ErrorHandlingExtensions));
							logger.LogError(contextFeature.Error, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
							
							context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
							errorModel = new ErrorModel
							{
								Message = "An unexpected error occurred",
								ErrorCode = "UnhandledExceptionError",
								StatusCode = 500
							};
							
							break;
						}
					}
					
					var json = JsonSerializer.Serialize(errorModel);
					await context.Response.WriteAsync(json);
					await context.Response.CompleteAsync();
				}
			});
		});
	}
}