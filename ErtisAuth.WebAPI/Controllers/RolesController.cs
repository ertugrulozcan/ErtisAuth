using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class RolesController : QueryControllerBase
	{
		#region Services

		private readonly IRoleService roleService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="roleService"></param>
		public RolesController(IRoleService roleService)
		{
			this.roleService = roleService;
		}

		#endregion

		#region Create Methods

		[HttpPost]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] Role model)
		{
			var role = await this.roleService.CreateAsync(membershipId, model);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{role.Id}", role);
		}

		#endregion
		
		#region Read Methods

		[HttpGet("{id}")]
		public async Task<ActionResult<Role>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var role = await this.roleService.GetAsync(membershipId, id);
			if (role != null)
			{
				return this.Ok(role);
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}
		
		[HttpGet]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var roles = await this.roleService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(roles);
		}

		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.roleService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] Role model)
		{
			model.Id = id;
			var role = await this.roleService.UpdateAsync(membershipId, model);
			return this.Ok(role);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			if (await this.roleService.DeleteAsync(membershipId, id))
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