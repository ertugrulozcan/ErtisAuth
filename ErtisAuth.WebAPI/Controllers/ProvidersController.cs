using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Attributes;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Extensions.AspNetCore.Extensions;
using ErtisAuth.Extensions.AspNetCore.Services;
using ErtisAuth.WebAPI.Models.Providers;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.WebAPI.Controllers;

[ApiController]
[Authorized]
[RbacResource("providers")]
[Route("memberships/{membershipId}/providers")]
public class ProvidersController : ControllerBase
{
	#region Services
	
	private readonly IProviderService _providerService;
	private readonly IUtilizerService _utilizerService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="providerService"></param>
	/// <param name="utilizerService"></param>
	public ProvidersController(IProviderService providerService, IUtilizerService utilizerService)
	{
		this._providerService = providerService;
		this._utilizerService = utilizerService;
	}
	
	#endregion
	
	#region Read Methods
	
	[HttpGet("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Read)]
	public async Task<ActionResult<Provider>> Get([FromRoute] string membershipId, [FromRoute] string id)
	{
		var provider = await this._providerService.GetAsync(membershipId, id);
		if (provider != null)
		{
			return this.Ok(provider);
		}
		else
		{
			return this.ApplicationNotFound(id);
		}
	}
	
	[HttpGet]
	[RbacAction(Rbac.CrudActions.Read)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Get([FromRoute] string membershipId, CancellationToken cancellationToken = default)
	{
		return this.Ok(await this._providerService.GetProvidersAsync(membershipId, cancellationToken: cancellationToken));
	}
	
	[HttpGet("active-providers")]
	[Unauthorized]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetActiveProviders([FromRoute] string membershipId)
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
	
	#region Update Methods
	
	[HttpPut("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Update)]
	public async Task<IActionResult> Update([FromRoute] string membershipId, [FromRoute] string id, [FromBody] UpdateProviderFormModel model, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(model.Name))
		{
			return this.BadRequest(ErtisAuthException.ProviderNameRequired().Error);
		}
		
		if (Enum.TryParse<KnownProviders>(model.Name, true, out var providerType) && providerType != KnownProviders.ErtisAuth)
		{
			var providerModel = new Provider(providerType)
			{
				Id = id,
				Description = model.Description,
				DefaultRole = model.DefaultRole,
				DefaultUserType = model.DefaultUserType,
				AppClientId = model.AppClientId,
				TenantId = model.TenantId,
				TeamId = model.TeamId,
				PrivateKey = model.PrivateKey,
				PrivateKeyId = model.PrivateKeyId,
				RedirectUri = model.RedirectUri,
				IsActive = model.IsActive ?? false,
				MembershipId = membershipId
			};
			
			var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
			var providerInstance = await this._providerService.UpdateAsync(utilizer, membershipId, providerModel, cancellationToken: cancellationToken);
			return this.Ok(providerInstance);
		}
		else
		{
			return this.BadRequest(ErtisAuthException.UnknownProvider(model.Name).Error);
		}
	}
	
	#endregion
	
	#region Delete Methods
	
	[HttpDelete("{id}")]
	[RbacObject("{id}")]
	[RbacAction(Rbac.CrudActions.Delete)]
	public async Task<IActionResult> Delete([FromRoute] string membershipId, [FromRoute] string id, CancellationToken cancellationToken = default)
	{
		var utilizer = await this._utilizerService.GetUtilizerAsync(this.User, cancellationToken: cancellationToken);
		if (await this._providerService.DeleteAsync(utilizer, membershipId, id, cancellationToken: cancellationToken))
		{
			return this.NoContent();
		}
		else
		{
			return this.ProviderNotFound(id);
		}
	}
	
	#endregion
}