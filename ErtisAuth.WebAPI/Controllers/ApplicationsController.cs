using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Extensions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.WebAPI.Models.Applications;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Applications")]
[Authorized]
[RbacResource("applications")]
[MembershipRoute("applications")]
public class ApplicationsController : QueryControllerBase
{
	#region Services
	
	private readonly IApplicationService _applicationService;
	private readonly IMembershipService _membershipService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="applicationService"></param>
	/// <param name="membershipService"></param>
	/// <param name="utilizerService"></param>
	public ApplicationsController(
		IApplicationService applicationService, 
		IMembershipService membershipService,
		IUtilizerService utilizerService)
	{
		this._applicationService = applicationService;
		this._membershipService = membershipService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get an application</summary>
	/// <remarks>The secret is never returned.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Application id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<Application>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<Application>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var app = id.IsObjectId() ? await this._applicationService.GetAsync(id, membershipId) : await this._applicationService.GetBySlugAsync(id, membershipId);
		if (app != null)
		{
			return this.Ok(app);
		}
		else
		{
			return this.ApplicationNotFound(id);
		}
	}
	
	/// <summary>List applications</summary>
	/// <remarks>Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Application>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var apps = await this._applicationService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(apps);
	}
	
	/// <summary>Query applications</summary>
	/// <remarks>Filters the applications with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Application>>(StatusCodes.Status200OK)]
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
			return await this._applicationService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	/// <summary>Search applications</summary>
	/// <remarks>Full text search with the <c>keyword</c> query parameter; paginated and sorted like the list endpoint.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="keyword">Text to search for</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("search")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<Application>>(StatusCodes.Status200OK)]
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
		
		return this.Ok(await this._applicationService.SearchAsync(keyword, membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken));
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create an application</summary>
	/// <remarks>Creates an application (machine to machine client) with a role and a newly generated secret. **Note:** the plain secret is returned only in this response; store it, it can not be read again (rotate it if it is lost).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="model">Application</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<ApplicationWithSecret>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateApplicationFormModel model, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			return this.MembershipNotFound(membershipId);
		}
		
		var applicationModel = new Application
		{ 
			Name = model.Name ?? string.Empty, 
			Slug = model.Slug ?? string.Empty,
			Role = model.Role ?? string.Empty,
			MembershipId = membershipId
		};
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var app = await this._applicationService.CreateWithSecretAsync(applicationModel, membershipId, utilizer, cancellationToken: cancellationToken);
		
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{app.Id}", app);
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update an application</summary>
	/// <remarks>Updates the name, slug and role of the application; the secret is not changed (see rotate secret). **Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Application id</param>
	/// <param name="model">Application</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<Application>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateApplicationFormModel model, CancellationToken cancellationToken = default)
	{
		var applicationModel = new Application
		{
			Id = id,
			Name = model.Name ?? string.Empty,
			Slug = model.Slug ?? string.Empty,
			Role = model.Role ?? string.Empty,
			MembershipId = membershipId
		};
		
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var app = await this._applicationService.UpdateAsync(applicationModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok(app);
	}
	
	/// <summary>Rotate the application secret</summary>
	/// <remarks>Generates a new secret for the application. **Note:** the previous secret is revoked immediately, and the plain new secret is returned only in this response.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Application id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("{id}/secret")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<ApplicationWithSecret>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> RotateSecret([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var app = await this._applicationService.RotateSecretAsync(id, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok(app);
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete an application</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Application id</param>
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
		if (await this._applicationService.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.ApplicationNotFound(id);
		}
	}
	
	/// <summary>Delete applications</summary>
	/// <remarks>Deletes the applications with the ids in the body. Returns 204 when all of them are deleted and 404 when none of them is deleted. **Note:** when only some of them are deleted the response is 200 with an error body (<c>BulkDeletePartial</c>), not a success.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="ids">Ids of the applications to delete</param>
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
			var isDeleted = await this._applicationService.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken);
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
}