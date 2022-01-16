using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers
{
	[Authorized]
	[RbacResource("providers")]
	[Route("providers")]
	public class ProvidersController : Controller
	{
		#region Index

		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		#endregion
	}
}