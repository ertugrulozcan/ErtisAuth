using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ErtisAuth.Hub.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Helpers;
using ErtisAuth.Hub.Services.Interfaces;
using ErtisAuth.Hub.ViewModels.Auth;
using IAuthenticationService = ErtisAuth.Sdk.Services.Interfaces.IAuthenticationService;

namespace ErtisAuth.Hub.Controllers
{
	public class AuthController : Controller
	{
		#region Services

		private readonly IAuthenticationService authenticationService;
		private readonly ISessionService sessionService;
		private readonly IPasswordService passwordService;
		private readonly ISuperUserOptions superUserOptions;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="authenticationService"></param>
		/// <param name="sessionService"></param>
		/// <param name="passwordService"></param>
		/// <param name="superUserOptions"></param>
		public AuthController(
			IAuthenticationService authenticationService, 
			ISessionService sessionService,
			IPasswordService passwordService,
			ISuperUserOptions superUserOptions)
		{
			this.authenticationService = authenticationService;
			this.sessionService = sessionService;
			this.passwordService = passwordService;
			this.superUserOptions = superUserOptions;
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
                var getTokenResult = await this.authenticationService.GetTokenAsync(model.Username?.Trim(), model.Password?.Trim(), ipAddress: model.ClientIP, userAgent: model.UserAgentRaw);
				if (getTokenResult.IsSuccess)
				{
					var loginResult = await this.sessionService.StartSessionAsync(this.HttpContext, getTokenResult.Data);
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
			var meResponse = await this.authenticationService.WhoAmIAsync(this.GetBearerToken());
			if (meResponse.IsSuccess)
			{
				return this.RedirectToAction("Forbidden", "Error");
			}
			else
			{
				this.ClearAllCookies();
				return this.View();
			}
		}

		#endregion
		
		#region Reset Password
		
		[HttpGet]
		[Route("reset-password")]
		[AllowAnonymous]
		public IActionResult ResetPassword([FromQuery] string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return this.RedirectToAction("Index", "Home");
			}

			var viewModel = new ResetPasswordViewModel();
			var resetPasswordTokenWithEmail = this.GetResetPasswordToken(token);
			if (string.IsNullOrEmpty(resetPasswordTokenWithEmail))
			{
				viewModel.IsSuccess = false;
				viewModel.ErrorMessage = "Your reset password token has expired or invalid, please again reset your password";
			}
			else
			{
				viewModel.IsSuccess = true;
				viewModel.ResetPasswordToken = token;
				var emailAddress = resetPasswordTokenWithEmail.Substring(0, resetPasswordTokenWithEmail.IndexOf(':'));
				viewModel.EmailAddress = emailAddress;
			}

			return this.View(viewModel);
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

			var resetPasswordTokenWithEmail = this.GetResetPasswordToken(model.ResetPasswordToken);
			if (!string.IsNullOrEmpty(resetPasswordTokenWithEmail))
			{
				var resetPasswordToken = resetPasswordTokenWithEmail.Substring(resetPasswordTokenWithEmail.IndexOf(':') + 1);
				var basicToken = this.superUserOptions.BasicToken;
				var setPasswordResponse = await this.passwordService.SetPasswordAsync(model.EmailAddress, model.NewPassword, resetPasswordToken, basicToken);
				if (setPasswordResponse.IsSuccess)
				{
					model.IsSuccess = true;
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
			}
			else
			{
				model.IsSuccess = false;
				model.ErrorMessage = "Your reset password token has expired or invalid, please again reset your password";
			}

			return this.View(model);
		}

		private string GetResetPasswordToken(string key)
		{
			if (!string.IsNullOrEmpty(key))
			{
				string resetPasswordTokenWithEmail = HttpUtility.UrlDecode(key, Encoding.UTF8);
				resetPasswordTokenWithEmail = resetPasswordTokenWithEmail.TrimStart('"');
				resetPasswordTokenWithEmail = resetPasswordTokenWithEmail.TrimEnd('"');
				return resetPasswordTokenWithEmail;
			}

			return key;
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