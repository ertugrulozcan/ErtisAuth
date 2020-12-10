using System.Net;
using Ertis.Core.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Extensions
{
	public static class ControllerExceptions
	{
		#region Methods

		public static NotFoundObjectResult UserNotFound(this ControllerBase controller, string userId)
		{
			return controller.NotFound(new ErrorModel
			{
				StatusCode = (int) HttpStatusCode.NotFound,
				Message = $"User not found in db by given _id: <{userId}>",
				ErrorCode = "UserNotFound"
			});
		}

		#endregion
	}
}