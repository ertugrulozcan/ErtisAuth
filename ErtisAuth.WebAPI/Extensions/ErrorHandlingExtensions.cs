using System.Collections.Generic;
using System.Linq;
using System.Net;
using Ertis.Core.Exceptions;
using Ertis.Core.Models.Response;
using Ertis.Schema.Exceptions;
using ErtisAuth.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Sentry;

namespace ErtisAuth.WebAPI.Extensions
{
	public static class ErrorHandlingExtensions
	{
		public static void ConfigureGlobalExceptionHandler(this IApplicationBuilder app)
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
										x.Message,
										x.FieldName,
										x.FieldPath
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
									ErrorCode = httpStatusCodeException.HelpLink,
									StatusCode = (int) httpStatusCodeException.StatusCode
								};
								break;
							case ErtisSchemaValidationException ertisSchemaValidationException:
								context.Response.StatusCode = 400;
								errorModel = contextFeature.Error switch
								{
									FieldValidationException fieldValidationException => new
									{
										fieldValidationException.Message,
										fieldValidationException.FieldName,
										fieldValidationException.FieldPath,
										ErrorCode = "FieldValidationException",
										StatusCode = 400
									},
									SchemaValidationException schemaValidationException => new
									{
										schemaValidationException.Message,
										ErrorCode = "SchemaValidationException",
										StatusCode = 400
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
								context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
								errorModel = new ErrorModel
								{
									Message = contextFeature.Error.Message,
									ErrorCode = "UnhandledExceptionError",
									StatusCode = 500
								};
								
								break;
						}
						
						var sentryEvent = new SentryEvent(contextFeature.Error)
						{
							Message = contextFeature.Error.Message
						};

						if (contextFeature.Error is ErtisAuthException { Extra: not null } ertisAuthException)
						{
							sentryEvent.SetExtras(ertisAuthException.Extra);
						}

						SentrySdk.CaptureEvent(sentryEvent);

						var json = Newtonsoft.Json.JsonConvert.SerializeObject(errorModel);
						await context.Response.WriteAsync(json);
						await context.Response.CompleteAsync();
					}
				});
			});
		}
	}
}