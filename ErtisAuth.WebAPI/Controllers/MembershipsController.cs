using System.Collections.Generic;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Controllers;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.WebAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/[controller]")]
	public class MembershipsController : QueryControllerBase
	{
		#region Services

		private readonly IMembershipService membershipService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		public MembershipsController(IMembershipService membershipService)
		{
			this.membershipService = membershipService;
		}

		#endregion
		
		#region Methods

		[HttpGet("{id}")]
		public async Task<IActionResult> Get([FromRoute] string id)
		{
			var membership = await this.membershipService.GetAsync(id);
			if (membership != null)
			{
				return this.Ok(membership);
			}
			else
			{
				return this.MembershipNotFound(id);
			}
		}
		
		[HttpGet]
		public async Task<IActionResult> Get()
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);
				
			var memberships = await this.membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(memberships);
		}
		
		protected override async Task<IPaginationCollection<dynamic>> GetDataAsync(string query, int? skip, int? limit, bool? withCount, string sortField, SortDirection? sortDirection, IDictionary<string, bool> selectFields)
		{
			return await this.membershipService.QueryAsync(query, skip, limit, withCount, sortField, sortDirection, selectFields);
		}

		#endregion
	}
}