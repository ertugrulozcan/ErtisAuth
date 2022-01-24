using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Helpers;
using ErtisAuth.Hub.Services.Interfaces;
using ErtisAuth.Hub.ViewModels.Auth;
using ErtisAuth.Sdk.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IAuthenticationService = ErtisAuth.Sdk.Services.Interfaces.IAuthenticationService;

namespace ErtisAuth.Hub.Controllers
{
	public class AuthController : Controller
	{
		#region Services

		private readonly IAuthenticationService authenticationService;
		private readonly ISessionService sessionService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="authenticationService"></param>
		/// <param name="sessionService"></param>
		public AuthController(IAuthenticationService authenticationService, ISessionService sessionService)
		{
			this.authenticationService = authenticationService;
			this.sessionService = sessionService;
		}

		#endregion
		
		#region Login

		[HttpGet]
		[Route("login")]
		[AllowAnonymous]
		public IActionResult Login()
		{
			return View();
		}
		
		[HttpPost]
		[Route("login")]
		[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] LoginViewModel model)
        {
            if (this.ModelState.IsValid)
            {
	            var serviceScopeFactory = this.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
	            using (var scope = serviceScopeFactory.CreateScope())
	            {
		            var scopedErtisAuthOptions = scope.ServiceProvider.GetRequiredService<IErtisAuthOptions>() as ScopedErtisAuthOptions;
		            scopedErtisAuthOptions?.SetTemporary(model.ServerUrl, model.MembershipId);
		            var scopedAuthenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		            
		            var getTokenResult = await scopedAuthenticationService.GetTokenAsync(model.Username?.Trim(), model.Password?.Trim(), ipAddress: model.ClientIP, userAgent: model.UserAgentRaw);
		            if (getTokenResult.IsSuccess)
		            {
			            var loginResult = await this.sessionService.StartSessionAsync(this.HttpContext, model.ServerUrl, model.MembershipId, getTokenResult.Data);
			            if (loginResult.IsSuccess)
			            {
				            if (!string.IsNullOrEmpty(model.ReturnUrl) && model.ReturnUrl != "/")
				            {
					            return this.Redirect(model.ReturnUrl);
				            }
						
				            return this.RedirectToAction("Index", "Home");
			            }
			            else
			            {
				            model.SetError(loginResult);
			            }
		            }
		            else
		            {
			            model.SetError(getTokenResult);
		            }
	            }
            }
            else
            {
	            model.SetError(this.ModelState, "Authentication failed");
            }

            return this.View(model);
		}

		#endregion
		
		#region Logout

		[Route("logout")]
		public async Task<IActionResult> Logout(string returnUrl, bool logoutOfAllDevices)
		{
			var accessTokenClaim = this.User.Claims.FirstOrDefault(x => x.Type == Claims.AccessToken);
			if (accessTokenClaim != null)
			{
				await this.authenticationService.RevokeTokenAsync(accessTokenClaim.Value, logoutOfAllDevices);	
			}
			
			var refreshTokenClaim = this.User.Claims.FirstOrDefault(x => x.Type == Claims.RefreshToken);
			if (refreshTokenClaim != null)
			{
				await this.authenticationService.RevokeTokenAsync(refreshTokenClaim.Value, logoutOfAllDevices);	
			}
			
			this.ClearAllCookies();
			
			await this.HttpContext.SignOutAsync();
			if (string.IsNullOrEmpty(returnUrl))
				return this.Redirect("/");
			else
				return this.Redirect(returnUrl);
		}

		#endregion

		#region Unauthorized (Forbidden)
		
		[HttpGet]
		[Route("unauthorized")]
		[AllowAnonymous]
		public async Task<IActionResult> Forbidden()
		{
			var serviceScopeFactory = this.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
			using (var scope = serviceScopeFactory.CreateScope())
			{
				if (scope.ServiceProvider.GetService<IErtisAuthOptions>() is ScopedErtisAuthOptions scopedErtisAuthOptions)
				{
					if (!string.IsNullOrEmpty(scopedErtisAuthOptions.BaseUrl) && !string.IsNullOrEmpty(scopedErtisAuthOptions.MembershipId))
					{
						var scopedAuthenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
						var meResponse = await scopedAuthenticationService.WhoAmIAsync(this.GetBearerToken());
						if (meResponse.IsSuccess)
						{
							return this.RedirectToAction("Forbidden", "Error");
						}	
					}
				}
			}
			
			this.ClearAllCookies();
			return this.View();
		}

		#endregion
		
		#region Set Password
		
		[HttpGet]
		[Route("set-password")]
		[AllowAnonymous]
		public IActionResult SetPassword([FromQuery] string token)
		{
			return this.View(ResetPasswordViewModel.Parse(token));
		}
		
		[HttpPost]
		[Route("set-password")]
		[AllowAnonymous]
		public async Task<IActionResult> SetPassword([FromForm] ResetPasswordViewModel model)
		{
			if (string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.NewPasswordAgain))
			{
				model.IsSuccess = false;
				model.ErrorMessage = "Please enter a new password";
				return this.View(model);
			}

			if (model.NewPassword != model.NewPasswordAgain)
			{
				model.IsSuccess = false;
				model.ErrorMessage = "Password and confirm password fields does not match";
				return this.View(model);
			}
			
			var serviceScopeFactory = this.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var scopedErtisAuthOptions = scope.ServiceProvider.GetRequiredService<IErtisAuthOptions>() as ScopedErtisAuthOptions;
				scopedErtisAuthOptions?.SetTemporary(model.ServerUrl, model.MembershipId);
				var scopedPasswordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
				
				var basicToken = new BasicToken($"ertisauth_server:{model.SecretKey}");
				var setPasswordResponse = await scopedPasswordService.SetPasswordAsync(model.EmailAddress, model.NewPassword, model.ResetPasswordToken, basicToken);
				if (setPasswordResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Password changed";
				}
				else if (ErrorHelper.TryGetError(setPasswordResponse, out var error))
				{
					model.IsSuccess = false;
					model.ErrorMessage = error.Message;
				}
				else
				{
					model.IsSuccess = false;
					model.ErrorMessage = setPasswordResponse.Message;
				}

				return this.View(model);
			}
		}

		#endregion
		
		#region Other Methods

		private void ClearAllCookies()
		{
			foreach (var claim in Claims.All)
			{
				this.Response.Cookies.Delete(claim);	
			}

			foreach (var cookie in this.Request.Cookies.Keys)
			{
				this.Response.Cookies.Delete(cookie);
			}
		}

		#endregion
	}
}