using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Events")]
[Authorized]
[RbacResource("events")]
[MembershipRoute("events")]
public class EventsController : QueryControllerBase
{
	#region Services
	
	private readonly IEventService _eventService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="eventService"></param>
	public EventsController(IEventService eventService)
	{
		this._eventService = eventService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get an event</summary>
	/// <remarks>Returns an event (user created, token generated, role updated...) with the utilizer who caused it and the related documents.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Event id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<ErtisAuthEvent>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ErtisAuthEvent>> Get([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var ertisAuthEvent = await this._eventService.GetAsync(id, membershipId, cancellationToken: cancellationToken);
		if (ertisAuthEvent != null)
		{
			return this.Ok(ertisAuthEvent);
		}
		else
		{
			return this.EventNotFound(id);
		}
	}
	
	/// <summary>List events</summary>
	/// <remarks>Returns the event log of the membership. Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<ErtisAuthEvent>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var events = await this._eventService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(events);
	}
	
	/// <summary>Query events</summary>
	/// <remarks>Filters the events with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<ErtisAuthEvent>>(StatusCodes.Status200OK)]
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
			return await this._eventService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	#endregion
}