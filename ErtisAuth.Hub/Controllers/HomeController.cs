using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Extensions.Authorization.Annotations;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
    public class HomeController : Controller
    {
        #region Index

        public IActionResult Index()
        {
            return View();
        }

        #endregion
    }
}