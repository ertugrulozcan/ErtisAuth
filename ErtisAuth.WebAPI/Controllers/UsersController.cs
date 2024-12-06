using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using Ertis.Schema.Dynamics.Legacy;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("users")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class UsersController : QueryControllerBase
	{
		#region Services

		private readonly IUserService _userService;
		private readonly ITokenService _tokenService;
		private readonly IOneTimePasswordService _oneTimePasswordService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userService"></param>
		/// <param name="tokenService"></param>
		/// <param name="oneTimePasswordService"></param>
		public UsersController(IUserService userService, ITokenService tokenService, IOneTimePasswordService oneTimePasswordService)
		{
			this._userService = userService;
			this._tokenService = tokenService;
			this._oneTimePasswordService = oneTimePasswordService;
		}

		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<ActionResult<User>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var user = await this._userService.GetAsync(membershipId, id);
			if (user != null)
			{
				user.RemoveProperty("password_hash");
				return this.Ok(user);
			}
			else
			{
				return this.UserNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var users = await this._userService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
			foreach (var user in users.Items)
			{
				user.RemoveProperty("password_hash");
			}
			
			return this.Ok(users);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
		{
			return await base.Query(cancellationToken: cancellationToken);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields, CancellationToken cancellationToken = default)
		{
			if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdSegment))
			{
				var membershipId = membershipIdSegment?.ToString();
				return await this._userService.QueryAsync(membershipId, query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);
			}
			else
			{
				return null;
			}
		}
		
		[HttpGet("search")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Search([FromRoute] string membershipId, [FromQuery] string keyword, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
			{
				return this.SearchKeywordRequired();
			}
			
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);
			
			return this.Ok(await this._userService.SearchAsync(membershipId, keyword, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
		}
		
		#endregion
		
		#region Create Methods
		
		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] DynamicObject model, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			var host = this.Request.Headers.TryGetValue("X-Host", out var hostStringValue) ? hostStringValue.ToString() : null;
			var user = await this._userService.CreateAsync(utilizer, membershipId, model, host, cancellationToken: cancellationToken);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{user["_id"]}", user);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] DynamicObject model, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			var user = await this._userService.UpdateAsync(utilizer, membershipId, id, model, cancellationToken: cancellationToken);
			return this.Ok(user);
		}
		
		#endregion

		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			if (await this._userService.DeleteAsync(utilizer, membershipId, id, cancellationToken: cancellationToken))
			{
				return this.NoContent();
			}
			else
			{
				return this.UserNotFound(id);
			}
		}
		
		[HttpDelete]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[] ids, CancellationToken cancellationToken = default)
		{
			return await this.BulkDeleteAsync(this._userService, membershipId, ids, cancellationToken: cancellationToken);
		}

		#endregion

		#region User Activation Methods
		
		[HttpGet("{id}/activate")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> ManualActivateUser([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			return this.Ok(await this._userService.ActivateUserByIdAsync(utilizer, membershipId, id, cancellationToken: cancellationToken));
		}
		
		[HttpGet("{id}/freeze")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> ManualFreezeUser([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			await this._tokenService.RevokeAllAsync(membershipId, id, cancellationToken: cancellationToken);
			return this.Ok(await this._userService.FreezeUserByIdAsync(utilizer, membershipId, id, cancellationToken: cancellationToken));
		}
		
		[HttpGet("activation")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> ActivateUser([FromRoute] string membershipId, [FromQuery] string uat, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			return this.Ok(await this._userService.ActivateUserAsync(utilizer, membershipId, uat, cancellationToken: cancellationToken));
		}
		
		[HttpPost("resend-activation-mail")]
		[RbacAction(Rbac.CrudActions.Create)]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> ResendActivationMail([FromRoute] string membershipId, [FromBody] ResendActivationMailFormModel model, CancellationToken cancellationToken = default)
		{
			var host = this.Request.Headers.TryGetValue("X-Host", out var hostStringValue) ? hostStringValue.ToString() : null;
			if (string.IsNullOrEmpty(host))
			{
				return this.HostRequired();
			}

			var user = await this._userService.GetByUsernameOrEmailAddressAsync(membershipId, model.EmailAddress);
			if (user == null)
			{
				return this.UserNotFound(model.EmailAddress);
			}
			
			var emailAddress = await this._userService.SendActivationMailAsync(membershipId, user.Id, host, cancellationToken: cancellationToken);
			if (string.IsNullOrEmpty(emailAddress))
			{
				return this.Ok(new
				{
					emailAddress
				});
			}
			else
			{
				return this.Unauthorized("Activation mail could not be sent");
			}
		}

		#endregion
		
		#region Change Password

		[HttpPut("{id}/change-password")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> ChangePassword([FromRoute] string membershipId, [FromRoute] string id, [FromBody] ChangePasswordFormModel model, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			await this._userService.ChangePasswordAsync(utilizer, membershipId, id, model.Password, cancellationToken: cancellationToken);
			return this.Ok();
		}

		#endregion
		
		#region Forgot Password
		
		[HttpPost("reset-password", Order = 1)]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> ResetPassword([FromRoute] string membershipId, [FromBody] ResetPasswordFormModel model, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			var host = this.Request.Headers.TryGetValue("X-Host", out var hostStringValue) ? hostStringValue.ToString() : null;
			if (string.IsNullOrEmpty(host))
			{
				return this.HostRequired();
			}
			
			var resetPasswordToken = await this._userService.ResetPasswordAsync(utilizer, membershipId, model.EmailAddress, host, cancellationToken: cancellationToken);
			return this.Ok(new
			{
				message = "Reset token generated",
				expiresIn = resetPasswordToken.ExpiresIn
			});
		}
		
		[HttpGet("verify-reset-token")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> VerifyResetToken([FromRoute] string membershipId, [FromQuery] string token, CancellationToken cancellationToken = default)
		{
			var user = await this._userService.VerifyResetTokenAsync(membershipId, token, cancellationToken: cancellationToken);
			return this.Ok(new
			{
				email_address = user.EmailAddress
			});
		}
		
		[HttpPost("set-password", Order = 3)]
		[RbacAction(Rbac.CrudActions.Update)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> SetPassword([FromRoute] string membershipId, [FromBody] SetPasswordFormModel model, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			await this._userService.SetPasswordAsync(utilizer, membershipId, model.ResetToken, model.UsernameOrEmailAddress, model.Password, cancellationToken: cancellationToken);
			await this._oneTimePasswordService.RevokeResetPasswordTokenAsync(utilizer, membershipId, model.ResetToken, cancellationToken: cancellationToken);
			return this.Ok();
		}

		#endregion
		
		#region Check Password

		[HttpGet("check-password")]
		[RbacAction(Rbac.CrudActions.Read)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> CheckPassword([FromRoute] string membershipId, [FromQuery] string password, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			if (membershipId != utilizer.MembershipId)
			{
				return this.Unauthorized();
			}
			
			if (await this._userService.CheckPasswordAsync(utilizer, password, cancellationToken: cancellationToken))
			{
				return this.Ok();
			}
			else
			{
				return this.Unauthorized("Invalid password");
			}
		}

		#endregion
		
		#region OTP Methods
		
		[HttpGet("{id}/generate-otp")]
		[RbacObject("{id}")]
		[RbacResource("otp")]
		[RbacAction(Rbac.CrudActions.Create)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> GenerateOneTimePassword([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
		{
			var utilizer = this.GetUtilizer();
			var otp = await this._oneTimePasswordService.GenerateAsync(utilizer, membershipId, id, cancellationToken: cancellationToken);
			return this.Ok(otp);
		}

		#endregion
	}
}