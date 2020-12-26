using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class UsersController : QueryControllerBase
	{
		#region Services

		private readonly IUserService userService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userService"></param>
		public UsersController(IUserService userService)
		{
			this.userService = userService;
		}

		#endregion
		
		#region Create Methods
		
		[HttpPost]
		public async Task<IActionResult> Post([FromRoute] string membershipId, [FromBody] User model)
		{
			var user = await this.userService.CreateAsync(membershipId, model);
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

		

		#endregion
		
		#region Delete Methods

		

		#endregion
	}
}