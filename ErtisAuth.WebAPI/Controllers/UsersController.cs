using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.WebAPI.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Users;
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

		private readonly IUserService userService;
		private readonly IMembershipService membershipService;
		private readonly ICryptographyService cryptographyService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userService"></param>
		/// <param name="membershipService"></param>
		/// <param name="cryptographyService"></param>
		public UsersController(IUserService userService, IMembershipService membershipService, ICryptographyService cryptographyService)
		{
			this.userService = userService;
			this.membershipService = membershipService;
			this.cryptographyService = cryptographyService;
		}

		#endregion
		
		#region Create Methods
		
		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateUserFormModel model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var userModel = new UserWithPassword
			{
				Username = model.Username,
				EmailAddress = model.EmailAddress,
				FirstName = model.FirstName,
				LastName = model.LastName,
				Role = model.Role,
				MembershipId = membershipId,
				PasswordHash = this.cryptographyService.CalculatePasswordHash(membership, model.Password)
			};
			
			var utilizer = this.GetUtilizer();
			var user = await this.userService.CreateAsync(utilizer, membershipId, userModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{user.Id}", user);
		}
		
		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<ActionResult<User>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var user = await this.userService.GetAsync(membershipId, id);
			if (user != null)
			{
				return this.Ok(user);
			}
			else
			{
				return this.UserNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var users = await this.userService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(users);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.userService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateUserFormModel model)
		{
			var userModel = new User
			{
				Id = id,
				Username = model.Username,
				EmailAddress = model.EmailAddress,
				FirstName = model.FirstName,
				LastName = model.LastName,
				Role = model.Role,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var user = await this.userService.UpdateAsync(utilizer, membershipId, userModel);
			return this.Ok(user);
		}
		
		#endregion

		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			var utilizer = this.GetUtilizer();
			if (await this.userService.DeleteAsync(utilizer, membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.UserNotFound(id);
			}
		}

		#endregion
		
		#region Change Password

		[HttpPut("{id}/change-password")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> ChangePassword([FromRoute] string membershipId, [FromRoute] string id, [FromBody] ChangePasswordFormModel model)
		{
			var utilizer = this.GetUtilizer();
			await this.userService.ChangePasswordAsync(utilizer, membershipId, id, model.Password);
			return this.Ok();
		}

		#endregion
		
		#region Forgot Password

		[HttpPost("reset-password", Order = 1)]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> ResetPassword([FromRoute] string membershipId, [FromBody] ResetPasswordFormModel model)
		{
			var utilizer = this.GetUtilizer();
			var resetPasswordToken = await this.userService.ResetPasswordAsync(utilizer, membershipId, model.UsernameOrEmailAddress);
			return this.Ok(resetPasswordToken);
		}
		
		[HttpPost("set-password", Order = 2)]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> SetPassword([FromRoute] string membershipId, [FromBody] SetPasswordFormModel model)
		{
			var utilizer = this.GetUtilizer();
			await this.userService.SetPasswordAsync(utilizer, membershipId, model.ResetToken, model.UsernameOrEmailAddress, model.Password);
			return this.Ok();
		}

		#endregion
	}
}