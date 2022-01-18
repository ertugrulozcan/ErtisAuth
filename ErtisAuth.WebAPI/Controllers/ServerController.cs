using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Extensions.AspNetCore.Extensions;
using ErtisAuth.Abstractions.Services.Interfaces;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/server")]
	public class ServerController : ControllerBase
	{
		#region Services

		private readonly IMembershipService membershipService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="membershipService"></param>
		public ServerController(IMembershipService membershipService)
		{
			this.membershipService = membershipService;
		}

		#endregion

		#region Membership Methods

		[HttpGet("memberships")]
		[RbacAction(Rbac.CrudActions.Read)]
		public async Task<IActionResult> GetMemberships()
		{
			this.ExtractPaginationParameters(out int? skip, out int? limit, out bool withCount);
			this.ExtractSortingParameters(out string orderBy, out SortDirection? sortDirection);

			var memberships = await this.membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection);
			return this.Ok(memberships);
		}

		#endregion
	}
}