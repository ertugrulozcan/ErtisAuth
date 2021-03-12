using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Infrastructure.Extensions;
using ErtisAuth.WebAPI.Annotations;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Models.Request.Roles;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("roles")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/[controller]")]
	public class RolesController : QueryControllerBase
	{
		#region Services

		private readonly IRoleService roleService;
		private readonly IMembershipService membershipService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="roleService"></param>
		/// <param name="membershipService"></param>
		public RolesController(IRoleService roleService, IMembershipService membershipService)
		{
			this.roleService = roleService;
			this.membershipService = membershipService;
		}

		#endregion

		#region Create Methods

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateRoleFormModel model)
		{
			var membership = await this.membershipService.GetAsync(membershipId);
			if (membership == null)
			{
				return this.MembershipNotFound(membershipId);
			}
			
			var roleModel = new Role
			{
				Name = model.Name,
				Description = model.Description,
				Permissions = model.Permissions,
				Forbidden = model.Forbidden,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var role = await this.roleService.CreateAsync(utilizer, membershipId, roleModel);
			return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{role.Id}", role);
		}

		#endregion
		
		#region Read Methods

		[HttpGet("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Read)]
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
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var roles = await this.roleService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(roles);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}

		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.roleService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}
		
		#endregion
		
		#region Update Methods

		[HttpPut("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateRoleFormModel model)
		{
			var roleModel = new Role
			{
				Id = id,
				Name = model.Name,
				Description = model.Description,
				Permissions = model.Permissions,
				Forbidden = model.Forbidden,
				MembershipId = membershipId
			};
			
			var utilizer = this.GetUtilizer();
			var role = await this.roleService.UpdateAsync(utilizer, membershipId, roleModel);
			return this.Ok(role);
		}

		#endregion
		
		#region Delete Methods

		[HttpDelete("{id}")]
		[RbacObject("id")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id)
		{
			var utilizer = this.GetUtilizer();
			if (await this.roleService.DeleteAsync(utilizer, membershipId, id))
			{
				return this.NoContent();
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}

		#endregion

		#region Region

		[HttpGet("{id}/check-permission")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> CheckPermissionByRole([FromRoute] string membershipId, [FromRoute] string id)
		{
			var role = await this.roleService.GetAsync(membershipId, id);
			if (role != null)
			{
				if (this.TryExtractPermissionParameter(out var rbac, out var errorModel))
				{
					if (role.HasPermission(rbac))
					{
						return this.Ok();
					}
					else
					{
						return this.Unauthorized();
					}
				}
				else
				{
					return this.BadRequest(errorModel);
				}
			}
			else
			{
				return this.RoleNotFound(id);
			}
		}
		
		[HttpGet("check-permission")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> CheckPermissionByToken([FromRoute] string membershipId)
		{
			var utilizer = this.GetUtilizer();
			var role = await this.roleService.GetByNameAsync(utilizer.Role, membershipId);
			if (role != null)
			{
				if (this.TryExtractPermissionParameter(out var rbac, out var errorModel))
				{
					if (role.HasPermission(rbac))
					{
						return this.Ok();
					}
					else
					{
						return this.Unauthorized();
					}
				}
				else
				{
					return this.BadRequest(errorModel);
				}
			}
			else
			{
				return this.RoleNotFound(utilizer.Role);
			}
		}

		private bool TryExtractPermissionParameter(out Rbac rbac, out ErrorModel errorModel)
		{
			if (this.Request.Query.ContainsKey("permission"))
			{
				var rbacString = this.Request.Query["permission"];
				if (Rbac.TryParse(rbacString, out rbac))
				{
					errorModel = null;
					return true;	
				}
				else
				{
					rbac = null;
					errorModel = new ErrorModel
					{
						Message = "The permission value is not valid as a rbac array.",
						ErrorCode = "InvalidRbac",
						StatusCode = 400
					};
				
					return false;
				}
			}
			else
			{
				rbac = null;
				errorModel = new ErrorModel
				{
					Message = "The permission parameter must be post in query string",
					ErrorCode = "PermissionParameterRequired",
					StatusCode = 400
				};
				
				return false;
			}
		}

		#endregion
	}
}