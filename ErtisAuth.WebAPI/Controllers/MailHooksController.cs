using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Mailing;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.WebAPI.Models.MailHooks;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Mailhooks")]
[Authorized]
[RbacResource("mailhooks")]
[MembershipRoute("mailhooks")]
public class MailHooksController : QueryControllerBase
{
    #region Services
	
	private readonly IMailHookService _mailHookService;
	private readonly IMembershipService _membershipService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mailHookService"></param>
	/// <param name="membershipService"></param>
	/// <param name="utilizerService"></param>
	public MailHooksController(
		IMailHookService mailHookService, 
		IMembershipService membershipService,
		IUtilizerService utilizerService)
	{
		this._mailHookService = mailHookService;
		this._membershipService = membershipService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get a mail hook</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Mail hook id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<MailHook>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<MailHook>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var mailHook = await this._mailHookService.GetAsync(id, membershipId);
		if (mailHook != null)
		{
			return this.Ok(mailHook);
		}
		else
		{
			return this.MailHookNotFound(id);
		}
	}
	
	/// <summary>List mail hooks</summary>
	/// <remarks>Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<MailHook>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var mailHooks = await this._mailHookService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(mailHooks);
	}
	
	/// <summary>Query mail hooks</summary>
	/// <remarks>Filters the mail hooks with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<MailHook>>(StatusCodes.Status200OK)]
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
			return await this._mailHookService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	#endregion
	
	#region Create Methods
	
	/// <summary>Create a mail hook</summary>
	/// <remarks>Sends a templated mail through a mail provider of the membership when the given event occurs, to the utilizer and/or the recipients.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="model">Mail hook</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost]
	[RbacAction(Rbac.CrudActions.Create)]
	[ProducesResponseType<MailHook>(StatusCodes.Status201Created)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromRoute] string membershipId, [FromBody] CreateMailHookFormModel model, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			return this.MembershipNotFound(membershipId);
		}
		
		var mailHookModel = model.ToMailHook(membershipId);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var mailHook = await this._mailHookService.CreateAsync(mailHookModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Created($"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}/{mailHook.Id}", mailHook);
	}
	
	#endregion
	
	#region Update Methods
	
	/// <summary>Update a mail hook</summary>
	/// <remarks>**Note:** an update without any change answers 409 (<c>IdenticalDocument</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Mail hook id</param>
	/// <param name="model">Mail hook</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	[ProducesResponseType<MailHook>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateMailHookFormModel model, CancellationToken cancellationToken = default)
	{
		// The route id, not an id in the body, identifies the updated mail hook (RBAC checks the route id)
		var mailHookModel = model.ToMailHook(id, membershipId);
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		var mailHook = await this._mailHookService.UpdateAsync(mailHookModel, membershipId, utilizer, cancellationToken: cancellationToken);
		return this.Ok(mailHook);
	}
	
	#endregion
	
	#region Delete Methods
	
	/// <summary>Delete a mail hook</summary>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Mail hook id</param>
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
		if (await this._mailHookService.DeleteAsync(id, membershipId, utilizer, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.MailHookNotFound(id);
		}
	}
	
	/// <summary>Delete mail hooks</summary>
	/// <remarks>Deletes the mail hooks with the ids in the body. Returns 204 when all of them are deleted and 404 when none of them is deleted. **Note:** when only some of them are deleted the response is 200 with an error body (<c>BulkDeletePartial</c>), not a success.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="ids">Ids of the mail hooks to delete</param>
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
			var isDeleted = await this._mailHookService.BulkDeleteAsync(ids, membershipId, utilizer, cancellationToken);
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