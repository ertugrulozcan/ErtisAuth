using System.Diagnostics;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Auth;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers
{
    public class ErrorController : Controller
    {
        #region Methods

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? code)
        {
            if (code != null)
            {
                if (code == 404)
                {
                    return this.RedirectToAction("Status404");
                }
                else if (code == 500)
                {
                    return this.RedirectToAction("Status500");
                }
            }
			
            var exceptionHandlerFeature = this.HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerFeature?.Error;
			
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Exception = exception });
        }

        [Route("notfound")]
        public IActionResult Status404()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
		
        [Route("internal-error")]
        public IActionResult Status500()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
		
        [Route("forbidden")]
        public IActionResult Forbidden()
        {
            var userId = this.HttpContext.GetClaim(Claims.UserId);
            if (!string.IsNullOrEmpty(userId))
            {
                var rbac = this.Request.Cookies.ContainsKey("rbac") ? this.Request.Cookies["rbac"] : null;
                var referer = this.Request.Headers.ContainsKey("Referer") ? this.Request.Headers["Referer"].ToString() : null;
                return this.View(new ForbiddenViewModel { Rbac = rbac, Referer = referer });
            }
            else
            {
                return this.RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }
}