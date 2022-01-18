using System.Threading.Tasks;
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
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);

			var getMembershipsResult = await this.membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection);
			if (getMembershipsResult?.Items != null)
			{
				foreach (var membership in getMembershipsResult.Items)
				{
					membership.SecretKey = Identity.Cryptography.StringCipher.Encrypt(membership.SecretKey, membership.Id);
				}
			}
			
			return this.Ok(getMembershipsResult);
		}

		#endregion
	}
}