using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Abstractions.Services;

namespace ErtisAuth.WebAPI.Controllers
{
	[ApiController]
	[Route("api/v{v:apiVersion}/memberships/{membershipId}/active-providers")]
	public class ProviderInfoController : ControllerBase
	{
		#region Services

		private readonly IProviderService providerService;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="providerService"></param>
		public ProviderInfoController(IProviderService providerService)
		{
			this.providerService = providerService;
		}

		#endregion

		#region Read Methods
		
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Get([FromRoute] string membershipId)
		{
			var providers = await this.providerService.GetProvidersAsync(membershipId);
			var activeProviders = providers.Where(x => x.IsActive != null && x.IsActive.Value);
			return this.Ok(activeProviders.Select(x => new
			{
				_id = x.Id,
				name = x.Name,
				appClientId = x.AppClientId,
				tenantId = x.TenantId,
				membership_id = x.MembershipId
			}));
		}

		#endregion
	}
}