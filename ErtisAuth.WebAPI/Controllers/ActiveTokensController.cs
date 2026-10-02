using Ertis.Core.Models;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Tags("Active Tokens")]
[Authorized]
[RbacResource("tokens")]
[MembershipRoute("active-tokens")]
public class ActiveTokensController : QueryControllerBase
{
	#region Services
	
	private readonly IActiveTokenService _activeTokenService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="activeTokenService"></param>
	public ActiveTokensController(IActiveTokenService activeTokenService)
	{
		this._activeTokenService = activeTokenService;
	}
	
	#endregion
	
	#region Read Methods
	
	/// <summary>Get an active token</summary>
	/// <remarks>Returns an access token which is neither expired nor revoked, with its owner and client info.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="id">Active token id</param>
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<ActiveToken>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ActiveToken>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var activeToken = await this._activeTokenService.GetAsync(id, membershipId);
		if (activeToken != null)
		{
			return this.Ok(activeToken);
		}
		else
		{
			return this.ActiveTokenNotFound(id);
		}
	}
	
	/// <summary>List active tokens</summary>
	/// <remarks>Returns the access tokens of the membership which are neither expired nor revoked. Paginated with the <c>skip</c>, <c>limit</c> and <c>with_count</c> query parameters, sorted with <c>sort</c> (e.g. <c>sort=name desc</c>).</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<ActiveToken>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
		this.ExtractSortingParameters(out var orderBy, out var sortDirection);
		
		var activeTokens = await this._activeTokenService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
		return this.Ok(activeTokens);
	}
	
	/// <summary>Query active tokens</summary>
	/// <remarks>Filters the active tokens with the MongoDB query in the <c>where</c> field of the body and projects them with <c>select</c>; paginated and sorted with the query parameters of the list endpoint. JavaScript operators (<c>$where</c>, <c>$function</c>) and hidden fields are rejected.</remarks>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_query")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType<PaginationCollection<ActiveToken>>(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
	{
		return await base.Query(cancellationToken: cancellationToken);
	}
	
	protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string? sortField, SortDirection? sortDirection, IDictionary<string, bool> projection, CancellationToken cancellationToken = default)
	{
		if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipIdValue) && membershipIdValue is string membershipId && !string.IsNullOrEmpty(membershipId))
		{
			return await this._activeTokenService.QueryAsync(query, membershipId, skip, limit, withCount, sortField, sortDirection, projection, cancellationToken: cancellationToken);	
		}
		else
		{
			throw ErtisAuthException.MembershipIdRequired();
		}
	}
	
	/// <summary>Aggregate active tokens</summary>
	/// <remarks>Runs the MongoDB aggregation pipeline (a JSON array of stages) in the body on the active tokens of the membership. Stages reading or writing other collections (<c>$lookup</c>, <c>$unionWith</c>, <c>$out</c>, <c>$merge</c>...) are rejected.</remarks>
	/// <param name="membershipId">Membership id</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpPost("_aggregate")]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<ErrorModel>(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Aggregate([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		var aggregationResults = await this._activeTokenService.AggregateAsync(await this.ExtractRequestBodyAsync(cancellationToken: cancellationToken), membershipId, cancellationToken: cancellationToken);
		return this.Ok(aggregationResults);
	}
	
	#endregion
}