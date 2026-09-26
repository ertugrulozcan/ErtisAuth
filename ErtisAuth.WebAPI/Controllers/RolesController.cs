using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.WebAPI.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Authorized]
[RbacResource("roles")]
[MembershipRoute("roles")]
public class RolesController : QueryControllerBase
{
	#region Services
	
	private readonly IRoleService _roleService;
	private readonly IAccessControlService _accessControlService;
	private readonly IMembershipService _membershipService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="roleService"></param>
	/// <param name="accessControlService"></param>
	/// <param name="membershipService"></param>
	/// <param name="utilizerService"></param>
	public RolesController(
		IRoleService roleService, 
		IAccessControlService accessControlService, 
		IMembershipService membershipService,
		IUtilizerService utilizerService)
	{
		this._roleService = roleService;
		this._accessControlService = accessControlService;
		this._membershipService = membershipService;
		this._utilizerService = utilizerService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Create Methods
	
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateRoleFormModel model, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			return this.MembershipNotFound(membershipId);
		}
		
		var roleModel = new Role
		{
			Name = model.Name ?? string.Empty,
			Slug = model.Slug ?? string.Empty,
			Description = model.Description,
			Permissions = model.Permissions,
			Forbidden = model.Forbidden,
			MembershipId = membershipId
		};
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var role = await this._roleService.CreateAsync(utilizer, membershipId, roleModel, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{role.Id}", role);
	}
	
	#endregion
	
	#region Read Methods
	
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	public async Task<ActionResult<Role>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var role = await this._roleService.GetAsync(membershipId, id);
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
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var roles = await this._roleService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(roles);
	}
	
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
	{
		return await base.Query(cancellationToken: cancellationToken);
	}
	
	protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields, CancellationToken cancellationToken = default)
	{
		if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdValue) && membershipIdValue is string membershipId && !string.IsNullOrEmpty(membershipId))
		{
			return await this._roleService.QueryAsync(membershipId, query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
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
		
		return this.Ok(await this._roleService.SearchAsync(membershipId, keyword, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
	}
	
	#endregion
	
	#region Update Methods
	
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateRoleFormModel model, CancellationToken cancellationToken = default)
	{
		var roleModel = new Role
		{
			Id = id,
			Name = model.Name ?? string.Empty,
			Slug = model.Slug ?? string.Empty,
			Description = model.Description,
			Permissions = model.Permissions,
			Forbidden = model.Forbidden,
			MembershipId = membershipId
		};
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var role = await this._roleService.UpdateAsync(utilizer, membershipId, roleModel, cancellationToken: cancellationToken);
		return this.Ok(role);
	}
	
	#endregion
	
	#region Delete Methods
	
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._roleService.DeleteAsync(utilizer, membershipId, id, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.RoleNotFound(id);
		}
	}
	
	[HttpDelete]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[RbacAction(Rbac.CrudActions.Delete)]
	public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[]? ids, CancellationToken cancellationToken = default)
	{
		if (ids != null)
		{
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			var isDeleted = await this._roleService.BulkDeleteAsync(utilizer, membershipId, ids, cancellationToken);
			if (isDeleted != null)
			{
				if (isDeleted.Value)
				{
					return this.NoContent();
				}
				else
				{
					return this.BulkDeleteFailed(ids);
				}
			}
			else
			{
				return this.BulkDeletePartial();
			}
		}
		else
		{
			return this.BadRequest();
		}
	}
	
	#endregion
	
	#region Check Permission
	
	[HttpGet("{id}/check-permission")]
	[RbacAction(Rbac.CrudActions.Read)]
	public async Task<IActionResult> CheckPermissionByRole([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var role = await this._roleService.GetAsync(membershipId, id, cancellationToken: cancellationToken);
		if (role != null)
		{
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			if (this.TryExtractPermissionParameter(out var rbac, out var errorModel))
			{
				if (rbac != null && this._accessControlService.HasPermission(role, rbac, utilizer))
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
	public async Task<IActionResult> CheckPermissionByToken([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var role = utilizer.Role != null ? await this._roleService.GetBySlugAsync(utilizer.Role, membershipId, cancellationToken: cancellationToken) : null;
		if (role != null)
		{
			if (this.TryExtractPermissionParameter(out var rbac, out var errorModel))
			{
				if (rbac != null && this._accessControlService.HasPermission(role, rbac, utilizer))
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
			return this.RoleNotFound(utilizer.Role ?? string.Empty);
		}
	}
	
	private bool TryExtractPermissionParameter(out Rbac? rbac, out ErrorModel? errorModel)
	{
		if (this.Request.Query.ContainsKey("permission"))
		{
			var rbacString = this.Request.Query["permission"];
			if (Rbac.TryParse(rbacString!, out rbac))
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