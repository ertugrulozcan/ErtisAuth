using System.Collections.Generic;
using System.Net;
using Ertis.Core.Exceptions;
using Ertis.Core.Models.Response;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Sentry;

namespace ErtisAuth.WebAPI.Extensions
{
	public static class ErrorHandlingExtensions
	{
		public static void ConfigureGlobalExceptionHandler(this IApplicationBuilder app, IHub sentryHub)
		{
			app.UseExceptionHandler(appError =>
			{
				appError.Run(async context =>
				{
					var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
					if (contextFeature?.Error != null)
					{
						context.Response.ContentType = "application/json";

						ErrorModel errorModel;
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
							default:
								context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
								errorModel = new ErrorModel
								{
									Message = contextFeature.Error.Message,
									ErrorCode = "UndhandledExceptionError",
									StatusCode = 500
								};
								break;
						}

						sentryHub?.CaptureException(contextFeature.Error);

						var json = Newtonsoft.Json.JsonConvert.SerializeObject(errorModel);
						await context.Response.WriteAsync(json);
					}
				});
			});
		}
	}
}