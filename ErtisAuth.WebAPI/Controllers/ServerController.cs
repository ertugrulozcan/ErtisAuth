using System.Linq;
using System.Threading;
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
		public async Task<IActionResult> GetMemberships(CancellationToken cancellationToken = default)
		{
			this.ExtractPaginationParameters(out var skip, out var limit, out var withCount);
			this.ExtractSortingParameters(out var orderBy, out var sortDirection);

			var getMembershipsResult = await this.membershipService.GetAsync(skip, limit, withCount, orderBy, sortDirection, cancellationToken: cancellationToken);
			if (getMembershipsResult?.Items != null)
			{
				return this.Ok(new PaginationCollection<dynamic>
				{
					Count = getMembershipsResult.Count,
					Items = getMembershipsResult.Items.Select(x => new
					{
						_id = x.Id,
						name = x.Name,
						slug = x.Slug,
						secret_key = Identity.Cryptography.StringCipher.Encrypt(x.SecretKey, x.Id)
					})
				});
			}

			return NotFound();
		}

		#endregion
	}
}