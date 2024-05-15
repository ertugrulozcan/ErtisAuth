using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.WebAPI.Extensions;
using ErtisAuth.WebAPI.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("tokens")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/active-tokens")]
	public class ActiveTokensController : QueryControllerBase
	{
		#region Services

		private readonly IActiveTokenService activeTokenService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="activeTokenService"></param>
		public ActiveTokensController(IActiveTokenService activeTokenService)
		{
			this.activeTokenService = activeTokenService;
		}

		#endregion
		
		#region Read Methods
		
		[HttpGet("{id}")]
		[RbacObject("{id}")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<ActionResult<ActiveToken>> Get([FromRoute] string membershipId, [FromRoute] string id)
		{
			var activeToken = await this.activeTokenService.GetAsync(membershipId, id);
			if (activeToken != null)
			{
				return this.Ok(activeToken);
			}
			else
			{
				return this.ActiveTokenNotFound(id);
			}
		}
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var activeTokens = await this.activeTokenService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
			return this.Ok(activeTokens);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query(CancellationToken cancellationToken = default)
		{
			return await base.Query(cancellationToken: cancellationToken);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields, CancellationToken cancellationToken = default)
		{
			if (this.Request.RouteValues.TryGetValue("membershipId", out var membershipId) && membershipId is string)
			{
				var dtos = await this.activeTokenService.QueryAsync(membershipId.ToString(), query, skip, limit, withCount, sortField, sortDirection, selectFields, cancellationToken: cancellationToken);
				return QueryHelper.FixTimeZoneOffsetInQueryResult(dtos);	
			}
			else
			{
				throw ErtisAuthException.MembershipIdRequired();
			}
		}
		
		[HttpPost("_aggregate")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Aggregate([FromRoute] string membershipId, CancellationToken cancellationToken = default)
		{
			var aggregationResults = await this.activeTokenService.AggregateAsync(membershipId, await this.ExtractRequestBodyAsync(cancellationToken: cancellationToken), cancellationToken: cancellationToken);
			return this.Ok(aggregationResults);
		}
		
		#endregion
	}
}