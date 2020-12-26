using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class UsersController : QueryControllerBase
	{
		#region Services

		private readonly IUserService userService;
		private readonly IMembershipService membershipService;
		private readonly ITokenService tokenService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userService"></param>
		/// <param name="membershipService"></param>
		/// <param name="tokenService"></param>
		public UsersController(IUserService userService, IMembershipService membershipService, ITokenService tokenService)
		{
			this.userService = userService;
			this.membershipService = membershipService;
			this.tokenService = tokenService;
		}

		#endregion
		
		#region Create Methods
		
		[HttpPost]
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
				PasswordHash = this.tokenService.CalculatePasswordHash(membership, model.Password)
			};
			
			var user = await this.userService.CreateAsync(membershipId, userModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{user.Id}", user);
		}
		
		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
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
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var users = await this.userService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(users);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.userService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateUserFormModel model)
		{
			var userModel = new User
			{
				Id = id,
				FirstName = model.FirstName,
				LastName = model.LastName,
				Role = model.Role,
				MembershipId = membershipId
			};
			
			var user = await this.userService.UpdateAsync(membershipId, userModel);
			return this.Ok(user);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			if (await this.userService.DeleteAsync(membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}

		#endregion
	}
}