using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Abstractions.Services;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Route("memberships/{membershipId}/active-providers")]
public class ProviderInfoController : ControllerBase
{
	#region Services
	
	private readonly IProviderService _providerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="providerService"></param>
	public ProviderInfoController(IProviderService providerService)
	{
		this._providerService = providerService;
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
		var providers = await this._providerService.GetProvidersAsync(membershipId);
		var activeProviders = providers.Where(x => x.IsActive);
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