using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.WebAPI.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Authorized]
	[RbacResource("tokens")]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/revoked-tokens")]
	public class RevokedTokensController : QueryControllerBase
	{
		#region Services

		private readonly IRevokedTokenService revokedTokenService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="revokedTokenService"></param>
		public RevokedTokensController(IRevokedTokenService revokedTokenService)
		{
			this.revokedTokenService = revokedTokenService;
		}

		#endregion
		
		#region Read Methods
		
		[HttpGet]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var revokedTokens = await this.revokedTokenService.GetAsync(membershipId, skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(revokedTokens);
		}
		
		[HttpPost("_query")]
		[RbacAction(Rbac.CrudActions.Read)]
		public override async Task<IActionResult> Query()
		{
			return await base.Query();
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			var dtos = await this.revokedTokenService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
			return QueryHelper.FixTimeZoneOffsetInQueryResult(dtos);
		}
		
		#endregion
	}
}