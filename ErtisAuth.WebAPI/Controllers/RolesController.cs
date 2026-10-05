using Ertis.Core.Collections;
using Ertis.Core.Models;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.WebAPI.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Roles")]
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
	
	#region Read Methods
	
	/// <summary>Get a role</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Role id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Role>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Role>> Get([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var role = id.IsObjectId() ? await this._roleService.GetAsync(id, membershipId, cancellationToken: cancellationToken) : await this._roleService.GetBySlugAsync(id, membershipId, cancellationToken: cancellationToken);
		if (role != null)
		{
			return this.Ok(role);
		}
		else
		{
			return this.RoleNotFound(id);
		}
	}
	
	/// <summary>List roles</summary>
	/// <remarks>Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Role>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var roles = await this._roleService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(roles);
	}
	
	/// <summary>Query roles</summary>
	/// <remarks>Filters the roles with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Role>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
	{
		return await base.Query(cancellationToken: cancellationToken);
	}
	
	[NonAction]
	protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string? sortField, SortDirection? sortDirection, IDictionary<string, bool> projection, CancellationToken cancellationToken = default)
	{
		if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdValue) && membershipIdValue is string membershipId && !string.IsNullOrEmpty(membershipId))
		{
			return await this._roleService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	/// <summary>Search roles</summary>
	/// <remarks>Full text search with the <c>keyword</c> query parameter; paginated and sorted like the list endpoint.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="keyword">Text to search for</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("search")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Role>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Search([FromRoute] string membershipId, [FromQuery] string keyword, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(keyword.Trim()))
		{
			return this.SearchKeywordRequired();
		}
		
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		return this.Ok(await this._roleService.SearchAsync(keyword, membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create a role</summary>
	/// <remarks>A role grants permissions (<c>permissions</c>) and forbids actions (<c>forbidden</c>) in the <c>[subject].[resource].[action].[object]</c> form.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="model">Role</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<Role>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
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
		var role = await this._roleService.CreateAsync(roleModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{role.Id}", role);
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a role</summary>
	/// <remarks>**Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Role id</param>
	/// <param name="model">Role</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<Role>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
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
		var role = await this._roleService.UpdateAsync(roleModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok(role);
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete a role</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Role id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._roleService.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.RoleNotFound(id);
		}
	}
	
	/// <summary>Delete roles</summary>
	/// <remarks>Deletes the roles with the ids in the body. Returns 204 when all of them are deleted and 404 when none of them is deleted. **Note:** when only some of them are deleted the response is 200 with an error body (<c>BulkDeletePartial</c>), not a success.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="ids">Ids of the roles to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpDelete]
	[RbacAction(Rbac.CrudActions.Delete)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> BulkDelete([FromRoute] string membershipId, [FromBody] string[]? ids, CancellationToken cancellationToken = default)
	{
		if (ids != null)
		{
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			var isDeleted = await this._roleService.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken);
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
	
	/// <summary>Check a permission of a role</summary>
	/// <remarks>Checks whether the role has the permission in the <c>permission</c> query parameter (<c>[subject].[resource].[action].[object]</c>, shorter forms like <c>users.read</c> allowed). **Note:** a denied permission answers 401, not 403.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Role id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("{id}/check-permission")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> CheckPermissionByRole([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var role = await this._roleService.GetAsync(id, membershipId, cancellationToken: cancellationToken);
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
	
	/// <summary>Check a permission of the caller</summary>
	/// <remarks>Checks whether the caller's token may perform the permission in the <c>permission</c> query parameter, with its role, user based permissions and token scopes. Needs only a valid token. Used by ErtisAuth.Sdk.AspNetCore. **Note:** a denied permission answers 401, not 403.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("check-permission")]
	[SelfAuthorized]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
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
	
	[NonAction]
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